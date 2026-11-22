using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CentroAether;

/// <summary>
/// Apariencia de las ventanas: barra de título oscura (Windows 10/11) y material Mica REAL de
/// Windows 11 (22H2 o más nuevo) con el compositor del sistema (DWM), no un color semitransparente.
/// Si Mica no está disponible (Windows 10 o 11 antiguo) se usa el fondo sólido oscuro de Aether.
/// </summary>
public static class Tema
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Margenes
    {
        public int Izq, Der, Arriba, Abajo;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margenes m);

    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int FondoMica = 2;

    /// <summary>Windows 11 22H2 (compilación 22621) o más nuevo: Mica disponible.</summary>
    public static bool MicaDisponible => Environment.OSVersion.Version.Build >= 22621;

    /// <summary>Mica de Windows 11 en esta ventana (o barra oscura + fondo sólido si no hay Mica).</summary>
    public static void Mica(Window v)
    {
        if (!MicaDisponible)
        {
            BarraOscura(v);
            return;
        }
        v.Background = System.Windows.Media.Brushes.Transparent;
        v.SourceInitialized += (_, _) =>
        {
            try
            {
                IntPtr h = new WindowInteropHelper(v).Handle;
                HwndSource? fuente = HwndSource.FromHwnd(h);
                if (fuente?.CompositionTarget is not null)
                {
                    fuente.CompositionTarget.BackgroundColor = System.Windows.Media.Colors.Transparent;
                }
                var m = new Margenes { Izq = -1, Der = -1, Arriba = -1, Abajo = -1 };
                DwmExtendFrameIntoClientArea(h, ref m);
                int si = 1;
                DwmSetWindowAttribute(h, DWMWA_USE_IMMERSIVE_DARK_MODE, ref si, sizeof(int));
                int tipo = FondoMica;
                if (DwmSetWindowAttribute(h, DWMWA_SYSTEMBACKDROP_TYPE, ref tipo, sizeof(int)) != 0)
                {
                    // No aceptó Mica: volver al fondo sólido para que nunca quede transparente.
                    v.SetResourceReference(Window.BackgroundProperty, "FondoSolidoBrush");
                }
            }
            catch
            {
                v.SetResourceReference(Window.BackgroundProperty, "FondoSolidoBrush");
            }
        };
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int valor, int tam);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_BORDER_COLOR = 34;

    /// <summary>Engancha el modo oscuro de la barra de título cuando la ventana tenga handle.</summary>
    public static void BarraOscura(Window v)
    {
        v.SourceInitialized += (_, _) => Aplicar(v);
    }

    private static void Aplicar(Window v)
    {
        try
        {
            IntPtr h = new WindowInteropHelper(v).Handle;
            if (h == IntPtr.Zero)
            {
                return;
            }
            int si = 1;
            if (DwmSetWindowAttribute(h, DWMWA_USE_IMMERSIVE_DARK_MODE, ref si, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(h, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref si, sizeof(int));
            }
            int caption = 0x00201410;              // #0C1B30 en 0x00BBGGRR
            int borde = 0x005C3A1E;                // #1E3A5C en 0x00BBGGRR
            DwmSetWindowAttribute(h, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
            DwmSetWindowAttribute(h, DWMWA_BORDER_COLOR, ref borde, sizeof(int));
        }
        catch { /* en Windows antiguos simplemente no aplica */ }
    }
}
