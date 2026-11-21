using Microsoft.UI.Xaml;

namespace AetherTransfer;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "AETHER TRANSFER";

        if (this.AppWindow is not null)
        {
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1300, 840));
            try
            {
                this.AppWindow.SetIcon("Assets\\aether-transfer.ico");
            }
            catch { /* el icono se verá por defecto si no carga */ }
        }
    }
}
