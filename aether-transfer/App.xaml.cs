using Microsoft.UI.Xaml;

namespace AetherTransfer;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();
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
            Program.Paso("OnLaunched EXCEPCIÓN: " + ex);
            throw;
        }
    }
}
