using System;
using System.Threading.Tasks;
using CentroAether;
using CentroAether.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

public sealed partial class ModosPage : Page
{
    public ModosPage()
    {
        this.InitializeComponent();
    }

    private string _modoActual = "Gaming";

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await RefrescarModo();
    }

    private async Task RefrescarModo()
    {
        if (!Aether.Disponible)
        {
            TxtModoActual.Text = "Centro no instalado";
            TxtModoDesc.Text = "No se encontro Aether.Centro.ps1 en este equipo.";
            return;
        }

        string modo = await Aether.ModoActual();
        _modoActual = modo;
        TxtModoActual.Text = $"Modo actual: {modo}";
        TxtModoDesc.Text = modo switch
        {
            "Gaming" => "Perfil optimizado para maximo rendimiento.",
            "Bateria" => "Perfil eficiente para durar mas con bateria.",
            _ => "Perfil equilibrado para el dia a dia.",
        };
    }

    private async void Trabajo_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => await Aplicar("Trabajo");
    private async void Gaming_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => await Aplicar("Gaming");
    private async void Bateria_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => await Aplicar("Bateria");

    private async Task Aplicar(string modo)
    {
        // "Cargando modo": se bloquean los botones y se avisa hasta que quede estable.
        BtnTrabajo.IsEnabled = BtnGaming.IsEnabled = BtnBateria.IsEnabled = false;
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = $"Cargando modo {modo}...";
        Estado.IsOpen = true;

        string salida = await Aether.CambiarModo(modo);

        await RefrescarModo();
        BtnTrabajo.IsEnabled = BtnGaming.IsEnabled = BtnBateria.IsEnabled = true;

        if (salida.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            Estado.Severity = InfoBarSeverity.Error;
            Estado.Message = "No se pudo cambiar de modo.";
        }
        else
        {
            Estado.Severity = InfoBarSeverity.Success;
            Estado.Message = $"Modo {modo} activo.";
            // Cada modo muestra su propio overlay (Gaming, Trabajo o Batería).
            VentanaOverlayModo.Mostrar(modo);
        }
        Estado.IsOpen = true;
    }

    private void VistaPreviaOverlay_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => VentanaOverlayModo.Mostrar(_modoActual);
}
