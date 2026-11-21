using Microsoft.UI.Xaml;

namespace AetherTransfer;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();

        // Diagnóstico: que WinUI nos avise QUÉ recurso o binding falla.
        try
        {
            this.DebugSettings.IsXamlResourceReferenceTracingEnabled = true;
            this.DebugSettings.IsBindingTracingEnabled = true;
            this.DebugSettings.XamlResourceReferenceFailed += (s, e) =>
                Program.Paso("XamlResourceReferenceFailed: " + e.Message);
            this.DebugSettings.BindingFailed += (s, e) =>
                Program.Paso("BindingFailed: " + e.Message);
        }
        catch (System.Exception ex) { Program.Paso("No se pudo activar tracing: " + ex.Message); }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Program.Paso("OnLaunched: inicio");
        try
        {
            Program.Paso("OnLaunched: antes de new MainWindow()");
            MainWindow = new MainWindow();
            Program.Paso("OnLaunched: MainWindow construida");
            MainWindow.Activate();
            Program.Paso("OnLaunched: Activate() OK");
        }
        catch (System.Exception ex)
        {
            Program.Paso($"OnLaunched EXCEPCIÓN: HRESULT=0x{ex.HResult:X8}");
            Program.Paso("Mensaje: " + ex.Message);
            if (ex.InnerException is not null)
                Program.Paso("Inner: " + ex.InnerException);
            Program.Paso("Full: " + ex);
            throw;
        }
    }
}
