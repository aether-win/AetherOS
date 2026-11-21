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

    public static bool Activo { get; private set; }

    /// <summary>Destino de conversión. Solo tiene efecto al (re)iniciar.</summary>
    public static TipoVirtual Objetivo { get; set; } = TipoVirtual.Xbox360;

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
        RawGameController? sony = RawGameController.RawGameControllers
            .FirstOrDefault(c => c.HardwareVendorId == VendorSony);
        if (sony is not null)
        {
            return LeerDs4(sony);
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

    // Índices del DualShock 4 vía RawGameController (afinables si tu mando difiere).
    private const int Ds4Square = 0, Ds4Cross = 1, Ds4Circle = 2, Ds4Triangle = 3;
    private const int Ds4L1 = 4, Ds4R1 = 5, Ds4L2 = 6, Ds4R2 = 7;
    private const int Ds4Share = 8, Ds4Options = 9, Ds4L3 = 10, Ds4R3 = 11, Ds4Ps = 12;
    private const int AxLX = 0, AxLY = 1, AxRX = 2, AxRY = 5;

    private static bool[] _botones = Array.Empty<bool>();
    private static GameControllerSwitchPosition[] _switches = Array.Empty<GameControllerSwitchPosition>();
    private static double[] _ejes = Array.Empty<double>();

    private static EstadoMando LeerDs4(RawGameController rc)
    {
        if (_botones.Length != rc.ButtonCount)
        {
            _botones = new bool[rc.ButtonCount];
        }
        if (_switches.Length != rc.SwitchCount)
        {
            _switches = new GameControllerSwitchPosition[rc.SwitchCount];
        }
        if (_ejes.Length != rc.AxisCount)
        {
            _ejes = new double[rc.AxisCount];
        }

        rc.GetCurrentReading(_botones, _switches, _ejes);

        bool Bt(int i) => i >= 0 && i < _botones.Length && _botones[i];
        double Ax(int i) => i >= 0 && i < _ejes.Length ? _ejes[i] : 0.5;
        double Centro(double v) => (Math.Clamp(v, 0.0, 1.0) * 2.0) - 1.0;

        GameControllerSwitchPosition sw = _switches.Length > 0 ? _switches[0] : GameControllerSwitchPosition.Center;

        return new EstadoMando
        {
            A = Bt(Ds4Cross),
            B = Bt(Ds4Circle),
            X = Bt(Ds4Square),
            Y = Bt(Ds4Triangle),
            L1 = Bt(Ds4L1),
            R1 = Bt(Ds4R1),
            L3 = Bt(Ds4L3),
            R3 = Bt(Ds4R3),
            Atras = Bt(Ds4Share),
            Inicio = Bt(Ds4Options),
            Guia = Bt(Ds4Ps),
            Up = sw is GameControllerSwitchPosition.Up or GameControllerSwitchPosition.UpLeft or GameControllerSwitchPosition.UpRight,
            Down = sw is GameControllerSwitchPosition.Down or GameControllerSwitchPosition.DownLeft or GameControllerSwitchPosition.DownRight,
            Left = sw is GameControllerSwitchPosition.Left or GameControllerSwitchPosition.UpLeft or GameControllerSwitchPosition.DownLeft,
            Right = sw is GameControllerSwitchPosition.Right or GameControllerSwitchPosition.UpRight or GameControllerSwitchPosition.DownRight,
            LX = Centro(Ax(AxLX)),
            LY = -Centro(Ax(AxLY)),   // RawGameController: Y invertido
            RX = Centro(Ax(AxRX)),
            RY = -Centro(Ax(AxRY)),
            LT = Bt(Ds4L2) ? 1.0 : 0.0,
            RT = Bt(Ds4R2) ? 1.0 : 0.0,
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
        d.SetButtonState(DualShock4Button.Ps, e.Guia);
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
