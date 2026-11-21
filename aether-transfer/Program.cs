using System;
using System.IO;
using System.Text;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace AetherTransfer;

// Punto de entrada propio SOLO para diagnóstico. Escribe una "miga de pan" en
// cada paso del arranque para saber EXACTAMENTE hasta dónde llega antes de morir.
public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Paso("PASO 0: Main inició");

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            Paso("UnhandledException: " + (e.ExceptionObject?.ToString() ?? "null"));
        AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
        {
            var m = e.Exception?.Message ?? "";
            if (e.Exception is System.Runtime.InteropServices.COMException
                || m.Contains("0x80004005") || m.Contains("combase")
                || m.Contains("XamlControls") || m.Contains("WindowsAppRuntime")
                || m.Contains("activat", StringComparison.OrdinalIgnoreCase))
                Paso("FirstChance(filtrada): " + e.Exception);
        };
        Paso("PASO 1: handlers registrados");

        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            Paso("PASO 2: InitializeComWrappers OK");

            Application.Start(p =>
            {
                Paso("PASO 3: dentro del callback de Application.Start");
                var contexto = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                System.Threading.SynchronizationContext.SetSynchronizationContext(contexto);
                Paso("PASO 4: antes de new App()");
                _ = new App();
                Paso("PASO 5: App construida OK");
            });
            Paso("PASO 6: Application.Start retornó");
        }
        catch (Exception ex)
        {
            Paso("EXCEPCIÓN en Main: " + ex);
            throw;
        }
    }

    private static readonly object _candado = new();

    public static void Paso(string mensaje)
    {
        try
        {
            var linea = $"[{DateTime.Now:HH:mm:ss.fff}] {mensaje}{Environment.NewLine}";
            lock (_candado)
            {
                foreach (var ruta in RutasLog())
                {
                    try
                    {
                        using var fs = new FileStream(ruta, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        var b = Encoding.UTF8.GetBytes(linea);
                        fs.Write(b, 0, b.Length);
                        fs.Flush(true); // fuerza a disco por si crashea justo después
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private static System.Collections.Generic.IEnumerable<string> RutasLog()
    {
        const string nombre = "aether-crash.log";
        yield return Path.Combine(AppContext.BaseDirectory, nombre);
        string? desk = null;
        try { desk = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); } catch { }
        if (!string.IsNullOrEmpty(desk))
            yield return Path.Combine(desk, "AetherTransfer-" + nombre);
        string? temp = null;
        try { temp = Path.GetTempPath(); } catch { }
        if (!string.IsNullOrEmpty(temp))
            yield return Path.Combine(temp, "AetherTransfer-" + nombre);
    }
}
