using CentroAether.Services;
using Microsoft.UI.Xaml;

namespace CentroAether;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();

        // Background Intelligence: solo si el usuario lo activó (apagado por defecto).
        InteligenciaFondo.Refrescar();
    }
}
