using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Windows.Gaming.Input;

namespace CentroAether.Services;

/// <summary>Tipo de mando virtual que presentamos a los juegos.</summary>
public enum TipoVirtual
{
    Xbox360,
    DualShock4,
}

/// <summary>Estado neutral de un mando, independiente del origen o destino.</summary>
public struct EstadoMando
{
    public bool A, B, X, Y, L1, R1, L3, R3, Atras, Inicio, Guia, Up, Down, Left, Right;
    public double LX, LY, RX, RY;   // -1..1, Y positivo = arriba
    public double LT, RT;           // 0..1
}

/// <summary>
/// Núcleo del Aether Controller: lee un mando físico (Sony en crudo o gamepad
/// estándar) y lo presenta a los juegos como un Xbox 360 o un DualShock 4 virtual
/// con ViGEm. Misma base técnica que DS4Windows (GPLv3). El driver ViGEmBus debe
/// estar instalado; conviene HidHide para ocultar el mando original.
/// </summary>
public static class MotorControlador
{
    private const ushort VendorSony = 0x054C;

    private static ViGEmClient? _client;
    private static IXbox360Controller? _x360;
    private static IDualShock4Controller? _ds4;
    private static CancellationTokenSource? _cts;
    private static Task? _bucle;
    private static LectorDs4Hid? _ds4Lector;

    // Rutas HID de los mandos VIRTUALES (ViGEm). Un DualShock 4 virtual también es "Sony", así
    // que nunca hay que leerlo como si fuera el real (antes pasaba: se leía a sí mismo).
    private static readonly HashSet<string> _rutasVirtuales = new(StringComparer.OrdinalIgnoreCase);
    // Mandos estándar (Xbox) que había ANTES de crear el virtual: el virtual nunca se lee.
    private static HashSet<string> _gamepadsReales = new(StringComparer.OrdinalIgnoreCase);
    private static string? _rutaReal;          // DualShock 4 real en uso (para reconectar el mismo)
    private static string? _rutaOculta;        // la que se ocultó con HidHide
    private static DateTime _proximoIntento = DateTime.MinValue;

    // Color de la barra de luz (azul jugador 1; los juegos pueden cambiarlo por el DS4 virtual).
    private static (byte R, byte G, byte B) _color = LuzDs4.Jugador1;
    private static volatile bool _colorCambio;

    public static bool Activo { get; private set; }

    // Mandos estándar que aparecieron con el virtual (el Xbox 360 de ViGEm): no son reales.
    private static readonly HashSet<string> _idsVirtuales = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True si esa ruta HID es un mando virtual de Aether (no se muestra ni se lee).</summary>
    public static bool EsRutaVirtual(string ruta) => _rutasVirtuales.Contains(ruta);

    /// <summary>True si ese mando de Windows.Gaming.Input es el virtual de Aether.</summary>
    public static bool EsIdVirtual(string id) => _idsVirtuales.Contains(id);

    public static IReadOnlyCollection<string> RutasVirtuales => _rutasVirtuales;

    /// <summary>Se dispara cuando la emulación se inicia o se detiene.</summary>
    public static event Action? EstadoCambiado;

    /// <summary>True si el mando real quedó oculto (los juegos solo ven el virtual).</summary>
    public static bool MandoRealOculto => _rutaOculta is not null;

    /// <summary>Destino de conversión. Solo tiene efecto al (re)iniciar.</summary>
    public static TipoVirtual Objetivo { get; set; } = TipoVirtual.Xbox360;

    private static double _deadzone = 0.10;
    private static double _sens = 1.0;

    /// <summary>Recarga zona muerta y sensibilidad desde la configuración.</summary>
    public static void CargarAjustes()
    {
        _deadzone = Math.Clamp(ConfigCentro.Deadzone, 0.0, 0.9);
        _sens = Math.Clamp(ConfigCentro.Sensibilidad, 0.1, 3.0);
    }

    /// <summary>Actualiza en caliente zona muerta y sensibilidad.</summary>
    public static void ActualizarAjustes(double deadzone, double sensibilidad)
    {
        _deadzone = Math.Clamp(deadzone, 0.0, 0.9);
        _sens = Math.Clamp(sensibilidad, 0.1, 3.0);
    }

    public static bool ViGEmDisponible()
    {
        try
        {
            using var c = new ViGEmClient();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool HaySony()
    {
        try
        {
            return RawGameController.RawGameControllers.Any(c => c.HardwareVendorId == VendorSony);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Devuelve el nombre de otro emulador de mandos en ejecución, o null.</summary>
    public static string? ConflictoActivo()
    {
        foreach (string nombre in new[] { "DS4Windows", "InputMapper", "x360ce" })
        {
            try
            {
                Process[] ps = Process.GetProcessesByName(nombre);
                bool hay = ps.Length > 0;
                foreach (Process p in ps)
                {
                    p.Dispose();
                }
                if (hay)
                {
                    return nombre;
                }
            }
            catch { /* siguiente */ }
        }
        return null;
    }

    public static bool Iniciar()
    {
        if (Activo)
        {
            return true;
        }
        try
        {
            CargarAjustes();
            _rutasVirtuales.Clear();
            _color = LuzDs4.Jugador1;

            // 1) Lo REAL primero (antes de crear el virtual, para no confundirlos).
            _gamepadsReales = new HashSet<string>(Gamepad.Gamepads.Select(IdGamepad).Where(i => i.Length > 0), StringComparer.OrdinalIgnoreCase);
            var idsAntes = new HashSet<string>(RawGameController.RawGameControllers.Select(c => c.NonRoamableId ?? ""), StringComparer.OrdinalIgnoreCase);
            var sonyAntes = new HashSet<string>(LectorDs4Hid.Buscar().Select(d => d.DevicePath), StringComparer.OrdinalIgnoreCase);
            var lector = new LectorDs4Hid();
            if (lector.Abrir())
            {
                _ds4Lector = lector;
                _rutaReal = lector.Ruta;
            }
            else
            {
                lector.Dispose();
                _ds4Lector = null;
                _rutaReal = null;
            }

            // 2) El mando virtual.
            _client = new ViGEmClient();
            if (Objetivo == TipoVirtual.DualShock4)
            {
                _ds4 = _client.CreateDualShock4Controller();
                _ds4.AutoSubmitReport = false;
                _ds4.FeedbackReceived += (_, e) =>
                {
                    // El juego pidió otro color de luz (como en una PS4): pasarlo al mando real.
                    var c = e.LightbarColor;
                    if (c.Red != 0 || c.Green != 0 || c.Blue != 0)
                    {
                        _color = (c.Red, c.Green, c.Blue);
                        _colorCambio = true;
                    }
                };
                _ds4.Connect();
            }
            else
            {
                _x360 = _client.CreateXbox360Controller();
                _x360.AutoSubmitReport = false;
                _x360.Connect();
            }

            // 3) Anotar las rutas Sony y los mandos estándar que aparecieron con el virtual.
            Thread.Sleep(400);
            foreach (var d in LectorDs4Hid.Buscar())
            {
                if (!sonyAntes.Contains(d.DevicePath))
                {
                    _rutasVirtuales.Add(d.DevicePath);
                }
            }
            _idsVirtuales.Clear();
            foreach (RawGameController rc in RawGameController.RawGameControllers)
            {
                string id = rc.NonRoamableId ?? "";
                if (id.Length > 0 && !idsAntes.Contains(id))
                {
                    _idsVirtuales.Add(id);
                }
            }

            // 4) Luz y ocultar el mando real: los juegos y Steam solo ven el virtual.
            if (_ds4Lector is not null)
            {
                _ds4Lector.EnviarLuz(_color.R, _color.G, _color.B);
                OcultarReal(_ds4Lector.Ruta);
            }

            _cts = new CancellationTokenSource();
            _bucle = Task.Run(() => Bucle(_cts.Token));
            Activo = true;
            ServicioMandos.Registrar($"Emulación iniciada como {(Objetivo == TipoVirtual.DualShock4 ? "DualShock 4" : "Xbox 360")}; mando real {(_ds4Lector is null ? "no encontrado por HID" : "abierto")}; oculto con HidHide: {(_rutaOculta is null ? "no" : "sí")}.");
            EstadoCambiado?.Invoke();
            return true;
        }
        catch
        {
            Detener();
            return false;
        }
    }

    private static void OcultarReal(string? ruta)
    {
        if (ruta is null || string.Equals(ruta, _rutaOculta, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        if (_rutaOculta is not null)
        {
            HidHideServicio.Mostrar(_rutaOculta);
            _rutaOculta = null;
        }
        if (HidHideServicio.Ocultar(ruta))
        {
            _rutaOculta = ruta;
        }
    }

    public static void Detener()
    {
        bool estaba = Activo;
        Activo = false;
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _bucle?.Wait(500); } catch { /* ignore */ }
        try { _x360?.Disconnect(); } catch { /* ignore */ }
        try { _ds4?.Disconnect(); } catch { /* ignore */ }
        try { _client?.Dispose(); } catch { /* ignore */ }
        try { _ds4Lector?.Cerrar(); } catch { /* ignore */ }
        // El mando real vuelve a verse normal.
        if (_rutaOculta is not null)
        {
            try { HidHideServicio.Mostrar(_rutaOculta); } catch { /* ignore */ }
            _rutaOculta = null;
        }
        _ds4Lector = null;
        _rutaReal = null;
        _rutasVirtuales.Clear();
        _gamepadsReales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _bucle = null;
        _cts?.Dispose();
        _cts = null;
        _x360 = null;
        _ds4 = null;
        _client = null;
        _idsVirtuales.Clear();
        if (estaba)
        {
            ServicioMandos.Registrar("Emulación detenida; el mando real vuelve a verse normal.");
            EstadoCambiado?.Invoke();
        }
    }

    private static void Bucle(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                EstadoMando? st = LeerEntrada();
                if (st is EstadoMando e)
                {
                    AjustarSticks(ref e);
                    if (_x360 is not null)
                    {
                        AplicarXbox360(e, _x360);
                        _x360.SubmitReport();
                    }
                    else if (_ds4 is not null)
                    {
                        AplicarDs4(e, _ds4);
                        _ds4.SubmitReport();
                    }
                }
            }
            catch
            {
                // Un fallo puntual de lectura no debe tumbar el bucle.
            }
            Thread.Sleep(8);   // ~125 Hz
        }
    }

    // ── Entrada ──────────────────────────────────────────────────────────────

    private static EstadoMando? LeerEntrada()
    {
        // DualShock 4 real por HID (fiable) si está abierto.
        if (_ds4Lector is { Abierto: true })
        {
            if (_colorCambio)
            {
                _colorCambio = false;
                _ds4Lector.EnviarLuz(_color.R, _color.G, _color.B);
            }
            EstadoMando? st = _ds4Lector.Leer(out bool desconectado);
            if (desconectado)
            {
                // Se cortó (p. ej. Bluetooth): cerrar y reconectar solo, sin tumbar el virtual.
                _ds4Lector.Cerrar();
                _proximoIntento = DateTime.UtcNow;
            }
            return st;
        }

        // Reconectar el DualShock 4 real (cada segundo) si se había cortado o se conectó después.
        if (DateTime.UtcNow >= _proximoIntento)
        {
            _proximoIntento = DateTime.UtcNow.AddSeconds(1);
            _ds4Lector ??= new LectorDs4Hid();
            if (_ds4Lector.Abrir(_rutasVirtuales, _rutaReal))
            {
                _rutaReal = _ds4Lector.Ruta;
                _ds4Lector.EnviarLuz(_color.R, _color.G, _color.B);
                OcultarReal(_rutaReal);
                return null;
            }
        }

        // Mando estándar (Xbox) real: solo los que existían antes de crear el virtual.
        Gamepad? gp = Gamepad.Gamepads.FirstOrDefault(g => _gamepadsReales.Contains(IdGamepad(g)));
        return gp is not null ? LeerGamepad(gp.GetCurrentReading()) : null;
    }

    /// <summary>Identificador estable de un mando (los objetos de WinRT cambian en cada consulta).</summary>
    private static string IdGamepad(Gamepad g)
    {
        try
        {
            return RawGameController.FromGameController(g)?.NonRoamableId ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static EstadoMando LeerGamepad(GamepadReading r)
    {
        GamepadButtons b = r.Buttons;
        return new EstadoMando
        {
            A = b.HasFlag(GamepadButtons.A),
            B = b.HasFlag(GamepadButtons.B),
            X = b.HasFlag(GamepadButtons.X),
            Y = b.HasFlag(GamepadButtons.Y),
            L1 = b.HasFlag(GamepadButtons.LeftShoulder),
            R1 = b.HasFlag(GamepadButtons.RightShoulder),
            L3 = b.HasFlag(GamepadButtons.LeftThumbstick),
            R3 = b.HasFlag(GamepadButtons.RightThumbstick),
            Atras = b.HasFlag(GamepadButtons.View),
            Inicio = b.HasFlag(GamepadButtons.Menu),
            Up = b.HasFlag(GamepadButtons.DPadUp),
            Down = b.HasFlag(GamepadButtons.DPadDown),
            Left = b.HasFlag(GamepadButtons.DPadLeft),
            Right = b.HasFlag(GamepadButtons.DPadRight),
            LX = r.LeftThumbstickX,
            LY = r.LeftThumbstickY,
            RX = r.RightThumbstickX,
            RY = r.RightThumbstickY,
            LT = r.LeftTrigger,
            RT = r.RightTrigger,
        };
    }


    // ── Salida: Xbox 360 ─────────────────────────────────────────────────────

    private static void AplicarXbox360(EstadoMando e, IXbox360Controller x)
    {
        x.SetButtonState(Xbox360Button.A, e.A);
        x.SetButtonState(Xbox360Button.B, e.B);
        x.SetButtonState(Xbox360Button.X, e.X);
        x.SetButtonState(Xbox360Button.Y, e.Y);
        x.SetButtonState(Xbox360Button.LeftShoulder, e.L1);
        x.SetButtonState(Xbox360Button.RightShoulder, e.R1);
        x.SetButtonState(Xbox360Button.LeftThumb, e.L3);
        x.SetButtonState(Xbox360Button.RightThumb, e.R3);
        x.SetButtonState(Xbox360Button.Back, e.Atras);
        x.SetButtonState(Xbox360Button.Start, e.Inicio);
        x.SetButtonState(Xbox360Button.Guide, e.Guia);
        x.SetButtonState(Xbox360Button.Up, e.Up);
        x.SetButtonState(Xbox360Button.Down, e.Down);
        x.SetButtonState(Xbox360Button.Left, e.Left);
        x.SetButtonState(Xbox360Button.Right, e.Right);

        x.SetAxisValue(Xbox360Axis.LeftThumbX, AShort(e.LX));
        x.SetAxisValue(Xbox360Axis.LeftThumbY, AShort(e.LY));
        x.SetAxisValue(Xbox360Axis.RightThumbX, AShort(e.RX));
        x.SetAxisValue(Xbox360Axis.RightThumbY, AShort(e.RY));
        x.SetSliderValue(Xbox360Slider.LeftTrigger, AByte01(e.LT));
        x.SetSliderValue(Xbox360Slider.RightTrigger, AByte01(e.RT));
    }

    // ── Salida: DualShock 4 ──────────────────────────────────────────────────

    private static void AplicarDs4(EstadoMando e, IDualShock4Controller d)
    {
        d.SetButtonState(DualShock4Button.Cross, e.A);
        d.SetButtonState(DualShock4Button.Circle, e.B);
        d.SetButtonState(DualShock4Button.Square, e.X);
        d.SetButtonState(DualShock4Button.Triangle, e.Y);
        d.SetButtonState(DualShock4Button.ShoulderLeft, e.L1);
        d.SetButtonState(DualShock4Button.ShoulderRight, e.R1);
        d.SetButtonState(DualShock4Button.ThumbLeft, e.L3);
        d.SetButtonState(DualShock4Button.ThumbRight, e.R3);
        d.SetButtonState(DualShock4Button.Share, e.Atras);
        d.SetButtonState(DualShock4Button.Options, e.Inicio);
        d.SetButtonState(DualShock4SpecialButton.Ps, e.Guia);
        d.SetButtonState(DualShock4Button.TriggerLeft, e.LT > 0.5);
        d.SetButtonState(DualShock4Button.TriggerRight, e.RT > 0.5);

        d.SetDPadDirection(Dpad(e));

        d.SetAxisValue(DualShock4Axis.LeftThumbX, AByteEje(e.LX, false));
        d.SetAxisValue(DualShock4Axis.LeftThumbY, AByteEje(e.LY, true));
        d.SetAxisValue(DualShock4Axis.RightThumbX, AByteEje(e.RX, false));
        d.SetAxisValue(DualShock4Axis.RightThumbY, AByteEje(e.RY, true));
        d.SetSliderValue(DualShock4Slider.LeftTrigger, AByte01(e.LT));
        d.SetSliderValue(DualShock4Slider.RightTrigger, AByte01(e.RT));
    }

    private static DualShock4DPadDirection Dpad(EstadoMando e)
    {
        if (e.Up && e.Right) { return DualShock4DPadDirection.Northeast; }
        if (e.Up && e.Left) { return DualShock4DPadDirection.Northwest; }
        if (e.Down && e.Right) { return DualShock4DPadDirection.Southeast; }
        if (e.Down && e.Left) { return DualShock4DPadDirection.Southwest; }
        if (e.Up) { return DualShock4DPadDirection.North; }
        if (e.Down) { return DualShock4DPadDirection.South; }
        if (e.Left) { return DualShock4DPadDirection.West; }
        if (e.Right) { return DualShock4DPadDirection.East; }
        return DualShock4DPadDirection.None;
    }

    // ── Zona muerta y sensibilidad ───────────────────────────────────────────

    private static void AjustarSticks(ref EstadoMando e)
    {
        (e.LX, e.LY) = ProcesarStick(e.LX, e.LY);
        (e.RX, e.RY) = ProcesarStick(e.RX, e.RY);
    }

    private static (double, double) ProcesarStick(double x, double y)
    {
        double mag = Math.Sqrt((x * x) + (y * y));
        if (mag <= _deadzone || mag <= 0.0)
        {
            return (0.0, 0.0);
        }

        // Reescalar fuera de la zona muerta a 0..1 y aplicar sensibilidad.
        double norm = (mag - _deadzone) / (1.0 - _deadzone);
        norm = Math.Min(norm * _sens, 1.0);
        return (x / mag * norm, y / mag * norm);
    }

    // ── Conversiones ─────────────────────────────────────────────────────────

    private static short AShort(double v)   // -1..1 -> short
        => (short)Math.Clamp(Math.Clamp(v, -1.0, 1.0) * 32767.0, short.MinValue, short.MaxValue);

    private static byte AByte01(double v)   // 0..1 -> byte
        => (byte)Math.Clamp(Math.Clamp(v, 0.0, 1.0) * 255.0, 0, 255);

    private static byte AByteEje(double v, bool invertir)   // -1..1 -> byte (128 centro)
    {
        double n = Math.Clamp(v, -1.0, 1.0);
        if (invertir)
        {
            n = -n;   // DS4: byte Y hacia abajo = 255
        }
        return (byte)Math.Clamp(((n + 1.0) / 2.0) * 255.0, 0, 255);
    }
}
