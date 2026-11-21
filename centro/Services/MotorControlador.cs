using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Windows.Gaming.Input;

namespace CentroAether.Services;

/// <summary>
/// Núcleo nativo del Aether Controller: crea un mando Xbox 360 virtual con ViGEm
/// y refleja en él la entrada del mando físico (DualShock 4, DualSense, etc. que
/// Windows exponga como gamepad). Misma base técnica que DS4Windows (GPLv3).
///
/// Requiere el driver ViGEmBus instalado. Para evitar entrada doble conviene
/// ocultar el mando original con HidHide (se ofrece instalar ambos desde la UI).
/// </summary>
public static class MotorControlador
{
    private static ViGEmClient? _client;
    private static IXbox360Controller? _x360;
    private static CancellationTokenSource? _cts;
    private static Task? _bucle;

    public static bool Activo { get; private set; }

    /// <summary>¿Está disponible el driver ViGEmBus?</summary>
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
            _x360 = _client.CreateXbox360Controller();
            _x360.AutoSubmitReport = false;
            _x360.Connect();

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
        try { _client?.Dispose(); } catch { /* ignore */ }
        _bucle = null;
        _cts?.Dispose();
        _cts = null;
        _x360 = null;
        _client = null;
    }

    private const ushort VendorSony = 0x054C;

    /// <summary>¿Hay un mando Sony (DS4/DualSense) conectado?</summary>
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

    private static void Bucle(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_x360 is not null)
                {
                    // Preferimos un mando Sony (PS4/PS5) leído en crudo; si no, un gamepad estándar.
                    RawGameController? sony = RawGameController.RawGameControllers
                        .FirstOrDefault(c => c.HardwareVendorId == VendorSony);

                    if (sony is not null)
                    {
                        MapearDs4(sony, _x360);
                        _x360.SubmitReport();
                    }
                    else
                    {
                        Gamepad? gp = Gamepad.Gamepads.FirstOrDefault();
                        if (gp is not null)
                        {
                            Mapear(gp.GetCurrentReading(), _x360);
                            _x360.SubmitReport();
                        }
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

    // Índices del DualShock 4 vía RawGameController (afinables si tu mando difiere).
    private const int Ds4Square = 0, Ds4Cross = 1, Ds4Circle = 2, Ds4Triangle = 3;
    private const int Ds4L1 = 4, Ds4R1 = 5, Ds4L2 = 6, Ds4R2 = 7;
    private const int Ds4Share = 8, Ds4Options = 9, Ds4L3 = 10, Ds4R3 = 11, Ds4Ps = 12;
    private const int AxLX = 0, AxLY = 1, AxRX = 2, AxRY = 5;

    private static bool[] _botones = Array.Empty<bool>();
    private static GameControllerSwitchPosition[] _switches = Array.Empty<GameControllerSwitchPosition>();
    private static double[] _ejes = Array.Empty<double>();

    private static void MapearDs4(RawGameController rc, IXbox360Controller x)
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

        x.SetButtonState(Xbox360Button.A, Bt(Ds4Cross));
        x.SetButtonState(Xbox360Button.B, Bt(Ds4Circle));
        x.SetButtonState(Xbox360Button.X, Bt(Ds4Square));
        x.SetButtonState(Xbox360Button.Y, Bt(Ds4Triangle));
        x.SetButtonState(Xbox360Button.LeftShoulder, Bt(Ds4L1));
        x.SetButtonState(Xbox360Button.RightShoulder, Bt(Ds4R1));
        x.SetButtonState(Xbox360Button.Back, Bt(Ds4Share));
        x.SetButtonState(Xbox360Button.Start, Bt(Ds4Options));
        x.SetButtonState(Xbox360Button.LeftThumb, Bt(Ds4L3));
        x.SetButtonState(Xbox360Button.RightThumb, Bt(Ds4R3));
        x.SetButtonState(Xbox360Button.Guide, Bt(Ds4Ps));

        GameControllerSwitchPosition sw = _switches.Length > 0 ? _switches[0] : GameControllerSwitchPosition.Center;
        x.SetButtonState(Xbox360Button.Up,
            sw is GameControllerSwitchPosition.Up or GameControllerSwitchPosition.UpLeft or GameControllerSwitchPosition.UpRight);
        x.SetButtonState(Xbox360Button.Down,
            sw is GameControllerSwitchPosition.Down or GameControllerSwitchPosition.DownLeft or GameControllerSwitchPosition.DownRight);
        x.SetButtonState(Xbox360Button.Left,
            sw is GameControllerSwitchPosition.Left or GameControllerSwitchPosition.UpLeft or GameControllerSwitchPosition.DownLeft);
        x.SetButtonState(Xbox360Button.Right,
            sw is GameControllerSwitchPosition.Right or GameControllerSwitchPosition.UpRight or GameControllerSwitchPosition.DownRight);

        // Sticks: RawGameController entrega 0..1 (0.5 = centro); el eje Y viene invertido.
        x.SetAxisValue(Xbox360Axis.LeftThumbX, AEje01(Ax(AxLX), false));
        x.SetAxisValue(Xbox360Axis.LeftThumbY, AEje01(Ax(AxLY), true));
        x.SetAxisValue(Xbox360Axis.RightThumbX, AEje01(Ax(AxRX), false));
        x.SetAxisValue(Xbox360Axis.RightThumbY, AEje01(Ax(AxRY), true));

        // Gatillos: por ahora digital (lleno/vacío) desde L2/R2 para garantizar que respondan.
        x.SetSliderValue(Xbox360Slider.LeftTrigger, (byte)(Bt(Ds4L2) ? 255 : 0));
        x.SetSliderValue(Xbox360Slider.RightTrigger, (byte)(Bt(Ds4R2) ? 255 : 0));
    }

    private static short AEje01(double v, bool invertir)
    {
        double centrado = (Math.Clamp(v, 0.0, 1.0) * 2.0) - 1.0;   // 0..1 -> -1..1
        if (invertir)
        {
            centrado = -centrado;
        }
        return (short)Math.Clamp(centrado * 32767.0, short.MinValue, short.MaxValue);
    }

    private static void Mapear(GamepadReading r, IXbox360Controller x)
    {
        GamepadButtons b = r.Buttons;

        x.SetButtonState(Xbox360Button.A, b.HasFlag(GamepadButtons.A));
        x.SetButtonState(Xbox360Button.B, b.HasFlag(GamepadButtons.B));
        x.SetButtonState(Xbox360Button.X, b.HasFlag(GamepadButtons.X));
        x.SetButtonState(Xbox360Button.Y, b.HasFlag(GamepadButtons.Y));
        x.SetButtonState(Xbox360Button.Start, b.HasFlag(GamepadButtons.Menu));
        x.SetButtonState(Xbox360Button.Back, b.HasFlag(GamepadButtons.View));
        x.SetButtonState(Xbox360Button.Up, b.HasFlag(GamepadButtons.DPadUp));
        x.SetButtonState(Xbox360Button.Down, b.HasFlag(GamepadButtons.DPadDown));
        x.SetButtonState(Xbox360Button.Left, b.HasFlag(GamepadButtons.DPadLeft));
        x.SetButtonState(Xbox360Button.Right, b.HasFlag(GamepadButtons.DPadRight));
        x.SetButtonState(Xbox360Button.LeftShoulder, b.HasFlag(GamepadButtons.LeftShoulder));
        x.SetButtonState(Xbox360Button.RightShoulder, b.HasFlag(GamepadButtons.RightShoulder));
        x.SetButtonState(Xbox360Button.LeftThumb, b.HasFlag(GamepadButtons.LeftThumbstick));
        x.SetButtonState(Xbox360Button.RightThumb, b.HasFlag(GamepadButtons.RightThumbstick));

        x.SetAxisValue(Xbox360Axis.LeftThumbX, AEje(r.LeftThumbstickX));
        x.SetAxisValue(Xbox360Axis.LeftThumbY, AEje(r.LeftThumbstickY));
        x.SetAxisValue(Xbox360Axis.RightThumbX, AEje(r.RightThumbstickX));
        x.SetAxisValue(Xbox360Axis.RightThumbY, AEje(r.RightThumbstickY));

        x.SetSliderValue(Xbox360Slider.LeftTrigger, AGatillo(r.LeftTrigger));
        x.SetSliderValue(Xbox360Slider.RightTrigger, AGatillo(r.RightTrigger));
    }

    private static short AEje(double v)
    {
        double s = Math.Clamp(v, -1.0, 1.0) * 32767.0;
        return (short)Math.Clamp(s, short.MinValue, short.MaxValue);
    }

    private static byte AGatillo(double v)
    {
        double s = Math.Clamp(v, 0.0, 1.0) * 255.0;
        return (byte)Math.Clamp(s, 0, 255);
    }
}
