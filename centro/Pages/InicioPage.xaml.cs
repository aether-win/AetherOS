using CentroAether.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

public sealed partial class InicioPage : Page
{
    public InicioPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (!Aether.Disponible)
        {
            TxtResumen.Text = "No se encontro la logica de Aether en este equipo.";
            return;
        }
        string modo = await Aether.ModoActual();
        string bateria = await Aether.Bateria();
        TxtResumen.Text = bateria == "SIN"
            ? $"Modo actual: {modo}."
            : $"Modo actual: {modo}  -  Bateria: {bateria}%";
    }

    private async void Oscuro_Click(object sender, RoutedEventArgs e)
    {
        await Aether.AplicarTema("oscuro");
        Aviso("Tema oscuro aplicado.");
    }

    private async void Claro_Click(object sender, RoutedEventArgs e)
    {
        await Aether.AplicarTema("claro");
        Aviso("Tema claro aplicado.");
    }

    private void Aviso(string texto)
    {
        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = texto;
        Estado.IsOpen = true;
    }
}
