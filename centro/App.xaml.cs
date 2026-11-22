using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using CentroAether.Services;

namespace CentroAether;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Si algo falla, queda escrito en CentroAether-aether-crash.log (Escritorio, %TEMP%
        // y junto al .exe) para poder ver la causa real, en vez de que la app solo se cierre.
        AppDomain.CurrentDomain.UnhandledException += (_, a) =>
            Registrar("UnhandledException", a.ExceptionObject as Exception);
        DispatcherUnhandledException += AlFallarInterfaz;

        base.OnStartup(e);

        var ventana = new MainWindow();
        MainWindow = ventana;
        ventana.Show();

        // Background Intelligence: solo si el usuario lo activó (apagado por defecto).
        InteligenciaFondo.Refrescar();
    }

    private void AlFallarInterfaz(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Registrar("Interfaz", e.Exception);
        // Un error en un botón no debe cerrar todo el Centro: se avisa y se sigue.
        e.Handled = true;
        try
        {
            MessageBox.Show(
                "Algo falló en el Centro Aether, pero puedes seguir usándolo.\n\n" + e.Exception.Message +
                "\n\nEl detalle quedó en el Escritorio (CentroAether-aether-crash.log).",
                "Centro Aether", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch { /* sin ventana disponible */ }
    }

    private static readonly object Candado = new();

    internal static void Registrar(string origen, Exception? ex)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== CENTRO AETHER: ERROR =====");
            sb.AppendLine($"Fecha:  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Origen: {origen}");
            sb.AppendLine($"OS:     {Environment.OSVersion}");
            sb.AppendLine($"Base:   {AppContext.BaseDirectory}");
            sb.AppendLine();
            sb.AppendLine(ex?.ToString() ?? "(excepción nula)");
            sb.AppendLine("================================");
            sb.AppendLine();

            string texto = sb.ToString();
            lock (Candado)
            {
                foreach (string ruta in RutasLog("aether-crash.log"))
                {
                    try { File.AppendAllText(ruta, texto); } catch { /* siguiente ruta */ }
                }
            }
        }
        catch { /* registrar nunca debe fallar */ }
    }

    private static IEnumerable<string> RutasLog(string nombre)
    {
        yield return Path.Combine(AppContext.BaseDirectory, nombre);
        string? escritorio = null;
        try { escritorio = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); } catch { }
        if (!string.IsNullOrEmpty(escritorio))
        {
            yield return Path.Combine(escritorio, "CentroAether-" + nombre);
        }
        string? temp = null;
        try { temp = Path.GetTempPath(); } catch { }
        if (!string.IsNullOrEmpty(temp))
        {
            yield return Path.Combine(temp, "CentroAether-" + nombre);
        }
    }
}
