using System;
using System.Threading;
using System.Windows;

namespace AetherTransfer;

/// <summary>
/// Una sola ventana de Aether Transfer: si ya está abierto, el doble clic trae al frente la
/// ventana que ya existe en vez de abrir otra.
/// </summary>
public partial class App : Application
{
    private const string NombreMutex = @"Local\AetherTransferApp";
    private const string NombreEvento = @"Local\AetherTransferMostrar";

    private Mutex? _mutex;
    private EventWaitHandle? _evento;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, NombreMutex, out bool primera);
        if (!primera)
        {
            try
            {
                using var otro = EventWaitHandle.OpenExisting(NombreEvento);
                otro.Set();
            }
            catch { /* la otra instancia está cerrando */ }
            Shutdown();
            return;
        }

        _evento = new EventWaitHandle(false, EventResetMode.AutoReset, NombreEvento);
        var hilo = new Thread(() =>
        {
            while (_evento.WaitOne())
            {
                Dispatcher.BeginInvoke(new Action(TraerAlFrente));
            }
        })
        { IsBackground = true, Name = "AetherTransfer-UnaInstancia" };
        hilo.Start();

        base.OnStartup(e);
        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    private void TraerAlFrente()
    {
        if (MainWindow is not { } v)
        {
            return;
        }
        if (v.WindowState == WindowState.Minimized)
        {
            v.WindowState = WindowState.Normal;
        }
        v.Show();
        v.Activate();
        v.Topmost = true;   // truco estándar para ganar el primer plano
        v.Topmost = false;
        v.Focus();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _mutex?.ReleaseMutex(); } catch { /* ya liberado */ }
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
