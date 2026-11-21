using System;
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

    private static void Bucle(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                Gamepad? gp = Gamepad.Gamepads.FirstOrDefault();
                if (gp is not null && _x360 is not null)
                {
                    Mapear(gp.GetCurrentReading(), _x360);
                    _x360.SubmitReport();
                }
            }
            catch
            {
                // Un fallo puntual de lectura no debe tumbar el bucle.
            }
            Thread.Sleep(8);   // ~125 Hz
        }
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
