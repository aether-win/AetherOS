using System.Threading.Tasks;
using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;

namespace CentroAether.Pages;

public sealed partial class ActualizarPage : PaginaBase
{
    private bool _cargando;

    public ActualizarPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _cargando = true;
        SwPausa.IsOn = await Aether.WindowsUpdateEnPausa();
        _cargando = false;
    }

    private async void SwPausa_Toggled(object sender, RoutedEventArgs e)
    {
        if (_cargando) return;
        bool pausar = SwPausa.IsOn;
        SwPausa.IsEnabled = false;
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = pausar ? "Pausando Windows Update..." : "Quitando la pausa...";
        Estado.IsOpen = true;

        await Aether.PausarWindowsUpdate(pausar);

        SwPausa.IsEnabled = true;
        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = pausar ? "Windows Update quedo en pausa." : "Windows Update activo.";
        Estado.IsOpen = true;
    }

    private async void BtnApps_Click(object sender, RoutedEventArgs e)
    {
        BtnApps.IsEnabled = false;
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Buscando y aplicando actualizaciones de tus programas...";
        Estado.IsOpen = true;

        await Aether.ActualizarProgramas();

        BtnApps.IsEnabled = true;
        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = "Listo. Tus programas quedaron al dia.";
        Estado.IsOpen = true;
    }

    private void EdgeGoogle_Click(object sender, System.Windows.RoutedEventArgs e) => Herramientas.EdgeGoogle();
}
