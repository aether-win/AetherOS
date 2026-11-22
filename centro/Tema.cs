using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CentroAether;

/// <summary>Aplica la barra de título oscura (modo inmersivo de Windows 10/11).</summary>
public static class Tema
{
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
