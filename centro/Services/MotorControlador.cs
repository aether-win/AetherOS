using System;
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

    public static bool Activo { get; private set; }

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
            _client = new ViGEmClient();

            if (Objetivo == TipoVirtual.DualShock4)
            {
                _ds4 = _client.CreateDualShock4Controller();
                _ds4.AutoSubmitReport = false;
                _ds4.Connect();
            }
            else
            {
                _x360 = _client.CreateXbox360Controller();
                _x360.AutoSubmitReport = false;
                _x360.Connect();
            }

            // Abrir el DualShock 4 por HID (si está conectado); si no, usaremos un gamepad.
            var lector = new LectorDs4Hid();
            if (lector.Abrir())
            {
                _ds4Lector = lector;
            }
            else
            {
                lector.Dispose();
                _ds4Lector = null;
            }

            _cts = new CancellationTokenSource();
            _bucle = Task.Run(() => Bucle(_cts.Token));
            Activo = true;
            return true;
        }
        catch
        {
            Detener();
            return false;
        }
    }

    public static void Detener()
    {
        Activo = false;
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _bucle?.Wait(500); } catch { /* ignore */ }
        try { _x360?.Disconnect(); } catch { /* ignore */ }
        try { _ds4?.Disconnect(); } catch { /* ignore */ }
        try { _client?.Dispose(); } catch { /* ignore */ }
        try { _ds4Lector?.Cerrar(); } catch { /* ignore */ }
        _ds4Lector = null;
        _bucle = null;
        _cts?.Dispose();
        _cts = null;
        _x360 = null;
        _ds4 = null;
        _client = null;
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
        // DualShock 4 por HID (fiable) si está abierto; si no, un gamepad estándar (Xbox).
        if (_ds4Lector is { Abierto: true })
        {
            return _ds4Lector.Leer();
        }

        Gamepad? gp = Gamepad.Gamepads.FirstOrDefault();
        return gp is not null ? LeerGamepad(gp.GetCurrentReading()) : null;
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
