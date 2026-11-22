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
    private static CancellationTokenSource? _cts;
    private static Task? _bucle;
    // Rutas HID de los mandos VIRTUALES (ViGEm). Un DualShock 4 virtual también es "Sony", así
    // que nunca hay que leerlo como si fuera el real (antes pasaba: se leía a sí mismo).
    private static readonly HashSet<string> _rutasVirtuales = new(StringComparer.OrdinalIgnoreCase);
    private static DateTime _proximaBusqueda = DateTime.MinValue;
    private const int MaxMandos = 4;

    /// <summary>
    /// Un mando real con su mando virtual. Hasta 4 a la vez (como una consola): cada uno con su
    /// color de jugador en la barra de luz y su propio mando emulado.
    /// </summary>
    private sealed class Ranura
    {
        public int Jugador;
        public LectorDs4Hid? Lector;          // DualShock 4 real por HID
        public string? RutaReal;              // para reconectar el mismo mando
        public string? RutaOculta;            // la que se ocultó con HidHide
        public string? GamepadId;             // mando estándar (Xbox) real
        public IXbox360Controller? X360;
        public IDualShock4Controller? Ds4;
        public (byte R, byte G, byte B) Color;
        public volatile bool ColorCambio;
        public DateTime DesconectadoDesde = DateTime.MinValue;
        public DateTime ProximoIntento = DateTime.MinValue;
    }

    private static readonly List<Ranura> _ranuras = new();
    private static readonly object _cerrojo = new();

    // Colores de jugador como en una PS4: azul, rojo, verde, rosa.
    private static readonly (byte R, byte G, byte B)[] ColoresJugador =
    {
        LuzDs4.Jugador1, (0xD8, 0x00, 0x10), (0x00, 0xC0, 0x30), (0xD8, 0x00, 0x90),
    };

    /// <summary>Cuántos mandos se están emulando ahora.</summary>
    public static int MandosEmulados { get { lock (_cerrojo) { return _ranuras.Count; } } }

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
    public static bool MandoRealOculto { get { lock (_cerrojo) { return _ranuras.Any(r => r.RutaOculta is not null); } } }

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
            _idsVirtuales.Clear();
            _client = new ViGEmClient();

            // Todos los mandos reales conectados: DualShock 4 (HID) y mandos estándar (Xbox).
            var sony = LectorDs4Hid.Buscar().Select(d => d.DevicePath).ToList();
            var estandar = Gamepad.Gamepads
                .Where(g => { try { return RawGameController.FromGameController(g)?.HardwareVendorId != VendorSony; } catch { return false; } })
                .Select(IdGamepad).Where(i => i.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            foreach (string ruta in sony)
            {
                if (_ranuras.Count >= MaxMandos) { break; }
                AgregarRanura(ruta, null);
            }
            foreach (string id in estandar)
            {
                if (_ranuras.Count >= MaxMandos) { break; }
                AgregarRanura(null, id);
            }
            if (_ranuras.Count == 0)
            {
                // Sin mandos todavía: el primero que se conecte se emula solo.
                ServicioMandos.Registrar("Emulación lista: esperando a que se conecte un mando.");
            }

            _cts = new CancellationTokenSource();
            _bucle = Task.Run(() => Bucle(_cts.Token));
            Activo = true;
            ServicioMandos.Registrar($"Emulación iniciada como {(Objetivo == TipoVirtual.DualShock4 ? "DualShock 4" : "Xbox 360")} para {_ranuras.Count} mando(s).");
            EstadoCambiado?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            ServicioMandos.Registrar("No se pudo iniciar la emulación: " + ex.Message);
            Detener();
            return false;
        }
    }

    /// <summary>Crea el mando virtual de un mando real (DualShock 4 por ruta HID o mando estándar por id).</summary>
    private static void AgregarRanura(string? rutaSony, string? gamepadId)
    {
        if (_client is null)
        {
            return;
        }
        var r = new Ranura { Jugador = SiguienteJugador(), GamepadId = gamepadId };
        r.Color = ColoresJugador[(r.Jugador - 1) % ColoresJugador.Length];

        if (rutaSony is not null)
        {
            var lector = new LectorDs4Hid();
            if (!lector.Abrir(RutasOcupadas(), rutaSony) || !string.Equals(lector.Ruta, rutaSony, StringComparison.OrdinalIgnoreCase))
            {
                lector.Dispose();
                return;
            }
            r.Lector = lector;
            r.RutaReal = lector.Ruta;
        }

        // Lo que hay ANTES del virtual, para anotar qué apareció con él (y no leerlo nunca).
        var sonyAntes = new HashSet<string>(LectorDs4Hid.Buscar().Select(d => d.DevicePath), StringComparer.OrdinalIgnoreCase);
        var idsAntes = new HashSet<string>(RawGameController.RawGameControllers.Select(c => c.NonRoamableId ?? ""), StringComparer.OrdinalIgnoreCase);

        if (Objetivo == TipoVirtual.DualShock4)
        {
            r.Ds4 = _client.CreateDualShock4Controller();
            r.Ds4.AutoSubmitReport = false;
            r.Ds4.FeedbackReceived += (_, e) =>
            {
                // El juego pidió otro color de luz (como en una PS4): pasarlo a ESTE mando real.
                var c = e.LightbarColor;
                if (c.Red != 0 || c.Green != 0 || c.Blue != 0)
                {
                    r.Color = (c.Red, c.Green, c.Blue);
                    r.ColorCambio = true;
                }
            };
            r.Ds4.Connect();
        }
        else
        {
            r.X360 = _client.CreateXbox360Controller();
            r.X360.AutoSubmitReport = false;
            r.X360.Connect();
        }

        Thread.Sleep(400);
        foreach (var d in LectorDs4Hid.Buscar())
        {
            if (!sonyAntes.Contains(d.DevicePath)) { _rutasVirtuales.Add(d.DevicePath); }
        }
        foreach (RawGameController rc in RawGameController.RawGameControllers)
        {
            string id = rc.NonRoamableId ?? "";
            if (id.Length > 0 && !idsAntes.Contains(id)) { _idsVirtuales.Add(id); }
        }

        if (r.Lector is not null)
        {
            r.Lector.EnviarLuz(r.Color.R, r.Color.G, r.Color.B);
            if (HidHideServicio.Ocultar(r.Lector.Ruta!)) { r.RutaOculta = r.Lector.Ruta; }
        }
        lock (_cerrojo) { _ranuras.Add(r); }
        ServicioMandos.Registrar($"Jugador {r.Jugador}: {(r.Lector is not null ? (r.Lector.EsBluetooth ? "DualShock 4 por Bluetooth" : "DualShock 4 por USB") : "mando estándar")} emulado; oculto con HidHide: {(r.RutaOculta is null ? "no" : "sí")}.");
        EstadoCambiado?.Invoke();
    }

    private static int SiguienteJugador()
    {
        for (int n = 1; n <= MaxMandos; n++)
        {
            lock (_cerrojo) { if (_ranuras.All(r => r.Jugador != n)) { return n; } }
        }
        return MaxMandos;
    }

    private static HashSet<string> RutasOcupadas()
    {
        var s = new HashSet<string>(_rutasVirtuales, StringComparer.OrdinalIgnoreCase);
        lock (_cerrojo)
        {
            foreach (var r in _ranuras) { if (r.RutaReal is not null) { s.Add(r.RutaReal); } }
        }
        return s;
    }

    private static void QuitarRanura(Ranura r)
    {
        try { r.X360?.Disconnect(); } catch { /* ignore */ }
        try { r.Ds4?.Disconnect(); } catch { /* ignore */ }
        try { r.Lector?.Cerrar(); } catch { /* ignore */ }
        if (r.RutaOculta is not null)
        {
            try { HidHideServicio.Mostrar(r.RutaOculta); } catch { /* ignore */ }
        }
        lock (_cerrojo) { _ranuras.Remove(r); }
        ServicioMandos.Registrar($"Jugador {r.Jugador}: mando retirado de la emulación.");
        EstadoCambiado?.Invoke();
    }

    public static void Detener()
    {
        bool estaba = Activo;
        Activo = false;
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _bucle?.Wait(500); } catch { /* ignore */ }
        List<Ranura> todas;
        lock (_cerrojo) { todas = _ranuras.ToList(); }
        foreach (var r in todas)
        {
            try { r.X360?.Disconnect(); } catch { /* ignore */ }
            try { r.Ds4?.Disconnect(); } catch { /* ignore */ }
            try { r.Lector?.Cerrar(); } catch { /* ignore */ }
            // El mando real vuelve a verse normal.
            if (r.RutaOculta is not null)
            {
                try { HidHideServicio.Mostrar(r.RutaOculta); } catch { /* ignore */ }
            }
        }
        lock (_cerrojo) { _ranuras.Clear(); }
        try { _client?.Dispose(); } catch { /* ignore */ }
        _rutasVirtuales.Clear();
        _idsVirtuales.Clear();
        _bucle = null;
        _cts?.Dispose();
        _cts = null;
        _client = null;
        if (estaba)
        {
            ServicioMandos.Registrar("Emulación detenida; los mandos reales vuelven a verse normal.");
            EstadoCambiado?.Invoke();
        }
    }

    private static void Bucle(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            List<Ranura> ranuras;
            lock (_cerrojo) { ranuras = _ranuras.ToList(); }
            foreach (var r in ranuras)
            {
                try
                {
                    EstadoMando? st = LeerEntrada(r);
                    if (st is EstadoMando e)
                    {
                        AjustarSticks(ref e);
                        if (r.X360 is not null)
                        {
                            AplicarXbox360(e, r.X360);
                            r.X360.SubmitReport();
                        }
                        else if (r.Ds4 is not null)
                        {
                            AplicarDs4(e, r.Ds4);
                            r.Ds4.SubmitReport();
                        }
                    }
                }
                catch
                {
                    // Un fallo puntual de lectura no debe tumbar el bucle.
                }
            }

            // Cada segundo: mandos nuevos (se emulan solos) y mandos que ya no vuelven (se retiran).
            if (DateTime.UtcNow >= _proximaBusqueda)
            {
                _proximaBusqueda = DateTime.UtcNow.AddSeconds(1);
                try { RevisarConexiones(ranuras); } catch { /* siguiente vuelta */ }
            }
            Thread.Sleep(ranuras.Count > 1 ? 4 : 8);   // ~125-250 Hz por mando
        }
    }

    private static void RevisarConexiones(List<Ranura> ranuras)
    {
        // Mandos que se fueron hace más de 60 s: se retira su virtual (no quedan fantasmas).
        foreach (var r in ranuras)
        {
            bool fuera = r.Lector is not null ? !r.Lector.Abierto
                : r.GamepadId is not null && !Gamepad.Gamepads.Any(g => string.Equals(IdGamepad(g), r.GamepadId, StringComparison.OrdinalIgnoreCase));
            if (!fuera)
            {
                r.DesconectadoDesde = DateTime.MinValue;
                continue;
            }
            if (r.DesconectadoDesde == DateTime.MinValue) { r.DesconectadoDesde = DateTime.UtcNow; }
            else if ((DateTime.UtcNow - r.DesconectadoDesde).TotalSeconds > 60) { QuitarRanura(r); }
        }

        int ocupadas;
        lock (_cerrojo) { ocupadas = _ranuras.Count; }
        if (ocupadas >= MaxMandos)
        {
            return;
        }
        // DualShock 4 nuevos (que no son virtuales ni ya tienen ranura).
        var usadas = RutasOcupadas();
        foreach (var d in LectorDs4Hid.Buscar(usadas))
        {
            // Si es la ruta de un mando que se cortó, lo reconecta su propia ranura.
            bool deAlguien;
            lock (_cerrojo) { deAlguien = _ranuras.Any(r => string.Equals(r.RutaReal, d.DevicePath, StringComparison.OrdinalIgnoreCase)); }
            if (!deAlguien) { AgregarRanura(d.DevicePath, null); }
            lock (_cerrojo) { if (_ranuras.Count >= MaxMandos) { return; } }
        }
        // Mandos estándar nuevos (no virtuales, no Sony, sin ranura).
        foreach (Gamepad g in Gamepad.Gamepads)
        {
            string id = IdGamepad(g);
            if (id.Length == 0 || _idsVirtuales.Contains(id)) { continue; }
            try { if (RawGameController.FromGameController(g)?.HardwareVendorId == VendorSony) { continue; } } catch { continue; }
            bool tiene;
            lock (_cerrojo) { tiene = _ranuras.Any(r => string.Equals(r.GamepadId, id, StringComparison.OrdinalIgnoreCase)); }
            if (!tiene) { AgregarRanura(null, id); }
            lock (_cerrojo) { if (_ranuras.Count >= MaxMandos) { return; } }
        }
    }

    // ── Entrada ──────────────────────────────────────────────────────────────

    private static EstadoMando? LeerEntrada(Ranura r)
    {
        if (r.Lector is not null)
        {
            if (r.Lector.Abierto)
            {
                if (r.ColorCambio)
                {
                    r.ColorCambio = false;
                    r.Lector.EnviarLuz(r.Color.R, r.Color.G, r.Color.B);
                }
                EstadoMando? st = r.Lector.Leer(out bool desconectado);
                if (desconectado)
                {
                    // Se cortó (p. ej. Bluetooth): cerrar y reconectar el MISMO mando, sin tumbar su virtual.
                    r.Lector.Cerrar();
                    r.ProximoIntento = DateTime.UtcNow;
                }
                return st;
            }
            if (DateTime.UtcNow >= r.ProximoIntento)
            {
                r.ProximoIntento = DateTime.UtcNow.AddSeconds(1);
                var otras = RutasOcupadas();
                if (r.RutaReal is not null) { otras.Remove(r.RutaReal); }
                if (r.Lector.Abrir(otras, r.RutaReal) && string.Equals(r.Lector.Ruta, r.RutaReal, StringComparison.OrdinalIgnoreCase))
                {
                    r.Lector.EnviarLuz(r.Color.R, r.Color.G, r.Color.B);
                    if (r.RutaOculta is null && HidHideServicio.Ocultar(r.RutaReal!)) { r.RutaOculta = r.RutaReal; }
                    ServicioMandos.Registrar($"Jugador {r.Jugador}: mando reconectado.");
                }
                else if (r.Lector.Abierto)
                {
                    r.Lector.Cerrar();   // abrió otro mando: no es este; lo tomará su propia ranura
                }
            }
            return null;
        }

        if (r.GamepadId is not null)
        {
            Gamepad? gp = Gamepad.Gamepads.FirstOrDefault(g => string.Equals(IdGamepad(g), r.GamepadId, StringComparison.OrdinalIgnoreCase));
            return gp is not null ? LeerGamepad(gp.GetCurrentReading()) : null;
        }
        return null;
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
