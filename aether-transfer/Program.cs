using System;
using System.IO;
using System.Text;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace AetherTransfer;

// Punto de entrada propio (reemplaza el Main autogenerado) SOLO para diagnóstico:
// envuelve todo el arranque en try/catch y escribe cualquier excepción —con su
// stack completo— a aether-crash.log. Así, si WinUI falla al activar, en vez de
// un "combase.dll E_FAIL" opaco tenemos el error real.
public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Captura TODO lo que no se maneje en ningún lado.
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            Registrar("UnhandledException", e.ExceptionObject as Exception);
        AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
            RegistrarPrimeraOportunidad(e.Exception);

        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            Application.Start(p =>
            {
                var contexto = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                System.Threading.SynchronizationContext.SetSynchronizationContext(contexto);
                _ = new App();
            });
        }
        catch (Exception ex)
        {
            Registrar("Main/Application.Start", ex);
            throw; // dejamos que el proceso termine, pero ya quedó registrado
        }
    }

    private static readonly object _candado = new();

    private static void Registrar(string origen, Exception? ex)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== AETHER TRANSFER CRASH =====");
            sb.AppendLine($"Fecha:  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Origen: {origen}");
            sb.AppendLine($"OS:     {Environment.OSVersion}");
            sb.AppendLine($"Base:   {AppContext.BaseDirectory}");
            sb.AppendLine();
            sb.AppendLine(ex?.ToString() ?? "(excepción nula)");
            sb.AppendLine("=================================");
            sb.AppendLine();

            var texto = sb.ToString();
            lock (_candado)
            {
                // Escribimos en varios sitios fáciles de encontrar.
                foreach (var ruta in RutasLog("aether-crash.log"))
                {
                    try { File.AppendAllText(ruta, texto); } catch { }
                }
            }
        }
        catch { /* el logging nunca debe tirar la app */ }
    }

    // Las excepciones de primera oportunidad son MUCHÍSIMAS en .NET normal,
    // así que solo guardamos las que mencionan COM/activación de WinUI.
    private static void RegistrarPrimeraOportunidad(Exception ex)
    {
        try
        {
            var m = ex.Message ?? "";
            if (ex is System.Runtime.InteropServices.COMException
                || m.Contains("0x80004005")
                || m.Contains("combase")
                || m.Contains("XamlControls")
                || m.Contains("WindowsAppRuntime")
                || m.Contains("activat", StringComparison.OrdinalIgnoreCase))
            {
                Registrar("FirstChance(filtrada)", ex);
            }
        }
        catch { }
    }

    private static System.Collections.Generic.IEnumerable<string> RutasLog(string nombre)
    {
        // 1) junto al .exe
        yield return Path.Combine(AppContext.BaseDirectory, nombre);
        // 2) escritorio del usuario
        string? desk = null;
        try { desk = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); } catch { }
        if (!string.IsNullOrEmpty(desk))
            yield return Path.Combine(desk, "AetherTransfer-" + nombre);
        // 3) %TEMP%
        string? temp = null;
        try { temp = Path.GetTempPath(); } catch { }
        if (!string.IsNullOrEmpty(temp))
            yield return Path.Combine(temp, "AetherTransfer-" + nombre);
    }
}
