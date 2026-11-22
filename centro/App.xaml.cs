using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CentroAether.Services;

namespace CentroAether;

public partial class App : Application
{
    private const string NombreMutex = @"Local\CentroAetherApp";
    private const string NombreSenal = @"Local\CentroAetherMostrar";

    private Mutex? _unico;
    private EventWaitHandle? _senal;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Una sola copia del Centro: si ya está en segundo plano, se le pide que muestre su
        // ventana y esta copia se cierra.
        _unico = new Mutex(true, NombreMutex, out bool soyElPrimero);
        if (!soyElPrimero && ReemplazarVersionAnterior())
        {
            // Había otra versión (otro .exe) abierta: se cerró y esta toma su lugar.
            _unico.Dispose();
            Thread.Sleep(300);
            _unico = new Mutex(true, NombreMutex, out soyElPrimero);
        }
        if (!soyElPrimero)
        {
            try { EventWaitHandle.OpenExisting(NombreSenal).Set(); } catch { /* la otra copia está cerrando */ }
            Shutdown();
            return;
        }
        _senal = new EventWaitHandle(false, EventResetMode.AutoReset, NombreSenal);
        ThreadPool.RegisterWaitForSingleObject(_senal,
            (_, _) => Dispatcher.BeginInvoke(new Action(MostrarCentro)), null, Timeout.Infinite, false);

        // Si algo falla, queda escrito en AetherToolbox-error.log (Escritorio, %TEMP%
        // y junto al .exe) para poder ver la causa real, en vez de que la app solo se cierre.
        AppDomain.CurrentDomain.UnhandledException += (_, a) =>
            Registrar("UnhandledException", a.ExceptionObject as Exception);
        DispatcherUnhandledException += AlFallarInterfaz;

        base.OnStartup(e);

        // Windows 11: material Mica real. Los fondos pasan a ser translúcidos para que se vea.
        if (Tema.MicaDisponible)
        {
            UsarMica();
        }

        // Segundo plano SIEMPRE activo: ícono en la bandeja, atajos y vigilancia del modo.
        SegundoPlano.Iniciar();

        // Al iniciar sesión se abre con --segundo-plano (solo la bandeja, sin ventana).
        bool soloSegundoPlano = e.Args.Any(a => a.Equals("--segundo-plano", StringComparison.OrdinalIgnoreCase));
        if (!soloSegundoPlano)
        {
            MostrarCentro();
        }

        // Background Intelligence: solo si el usuario lo activó (apagado por defecto).
        InteligenciaFondo.Refrescar();
    }

    /// <summary>
    /// Si la copia que ya está abierta es OTRO archivo (una versión anterior, p. ej. el viejo
    /// "CentroAether.exe" o un AetherToolbox.exe de otra carpeta), la cierra para que la versión
    /// nueva tome su lugar. Si es el mismo archivo, no hace nada (solo se mostrará su ventana).
    /// </summary>
    private static bool ReemplazarVersionAnterior()
    {
        bool cerro = false;
        string yo = Environment.ProcessPath ?? "";
        int miId = Environment.ProcessId;
        foreach (string nombre in new[] { "AetherToolbox", "CentroAether" })
        {
            foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcessesByName(nombre))
            {
                using (p)
                {
                    try
                    {
                        if (p.Id == miId)
                        {
                            continue;
                        }
                        string ruta = p.MainModule?.FileName ?? "";
                        if (string.Equals(ruta, yo, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        p.Kill();
                        p.WaitForExit(5000);
                        cerro = true;
                    }
                    catch { /* sin acceso a ese proceso */ }
                }
            }
        }
        return cerro;
    }

    private void UsarMica()
    {
        static System.Windows.Media.SolidColorBrush B(byte a, byte r, byte g, byte b) =>
            new(System.Windows.Media.Color.FromArgb(a, r, g, b));

        Resources["FondoVentanaBrush"] = System.Windows.Media.Brushes.Transparent;
        Resources["TarjetaFondoBrush"] = B(0xC8, 0x0F, 0x20, 0x36);
        Resources["LayerFillColorDefaultBrush"] = B(0x99, 0x0B, 0x1A, 0x2E);
        Resources["CardBackgroundFillColorDefaultBrush"] = B(0xC8, 0x0F, 0x20, 0x36);
        Resources["InfoBarFondoBrush"] = B(0xD8, 0x10, 0x23, 0x3B);
        var menu = new System.Windows.Media.LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        menu.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(0xA0, 0x0E, 0x21, 0x40), 0));
        menu.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(0x80, 0x0A, 0x16, 0x28), 1));
        Resources["MenuFondoBrush"] = menu;
    }

    /// <summary>Muestra (o crea) la ventana del Centro y la trae al frente.</summary>
    public void MostrarCentro()
    {
        if (MainWindow is not CentroAether.MainWindow ventana)
        {
            ventana = new CentroAether.MainWindow();
            MainWindow = ventana;
        }
        ventana.MostrarDesdeSegundoPlano();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SegundoPlano.Detener();
        MotorControlador.Detener();
        base.OnExit(e);
    }

    private void AlFallarInterfaz(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Registrar("Interfaz", e.Exception);
        // Un error en un botón no debe cerrar todo el Centro: se avisa y se sigue.
        e.Handled = true;
        // Aviso por notificación de Windows (nada de cuadros blancos).
        SegundoPlano.Notificar("Algo falló, pero Aether Toolbox sigue funcionando. El detalle quedó en el Escritorio (AetherToolbox-error.log).");
    }

    private static readonly object Candado = new();

    internal static void Registrar(string origen, Exception? ex)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== AETHER TOOLBOX: ERROR =====");
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
                foreach (string ruta in RutasLog("error.log"))
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
            yield return Path.Combine(escritorio, "AetherToolbox-" + nombre);
        }
        string? temp = null;
        try { temp = Path.GetTempPath(); } catch { }
        if (!string.IsNullOrEmpty(temp))
        {
            yield return Path.Combine(temp, "AetherToolbox-" + nombre);
        }
    }
}
