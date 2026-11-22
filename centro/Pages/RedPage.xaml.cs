using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;

namespace CentroAether.Pages;

public sealed partial class RedPage : PaginaBase
{
    public RedPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        TxtEquipo.Text = $"Este equipo: {Aether.NombreEquipo}";
    }

    private async void Reparar_Click(object sender, RoutedEventArgs e)
    {
        BtnReparar.IsEnabled = false;
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Reparando la red... esto puede tardar un momento.";
        Estado.IsOpen = true;

        await Aether.RepararRed();

        BtnReparar.IsEnabled = true;
        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = "Red revisada y reparada.";
        Estado.IsOpen = true;
    }

    private async void Privada_Click(object sender, RoutedEventArgs e)
    {
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Haciendo privada la red...";
        Estado.IsOpen = true;

        await Aether.HacerRedPrivada();

        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = "Las redes conectadas quedaron como privadas.";
        Estado.IsOpen = true;
    }

    private async void Compartir_Click(object sender, RoutedEventArgs e)
    {
        await Aether.AbrirUsoCompartido();
    }
}
