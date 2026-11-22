using System.Collections.Generic;
using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;

namespace CentroAether.Pages;

public sealed partial class WindPage : PaginaBase
{
    private bool _cargando;

    public WindPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        AvisoWindhawk.IsOpen = !Aether.WindhawkInstalado();

        _cargando = true;
        List<ModWind> mods = Aether.ListaMods();
        ListaMods.ItemsSource = mods;
        _cargando = false;
    }

    private async void Mod_Toggled(object sender, RoutedEventArgs e)
    {
        if (_cargando) return;
        if (sender is ToggleSwitch sw && sw.Tag is string id)
        {
            bool activo = sw.IsOn;
            await Aether.ActivarMod(id, activo);
            Estado.Severity = InfoBarSeverity.Success;
            Estado.Message = activo ? "Mod activado." : "Mod desactivado.";
            Estado.IsOpen = true;
        }
    }

    private async void InstalarWindhawk_Click(object sender, RoutedEventArgs e)
    {
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Instalando Windhawk...";
        Estado.IsOpen = true;
        await Aether.Ejecutar("winget install --id RamenSoftware.Windhawk --exact --accept-package-agreements --accept-source-agreements --disable-interactivity");
        AvisoWindhawk.IsOpen = !Aether.WindhawkInstalado();
        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = Aether.WindhawkInstalado() ? "Windhawk instalado." : "Revisa la instalacion de Windhawk.";
        Estado.IsOpen = true;
    }
}
