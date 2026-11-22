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
        bool cli = Aether.WindhawkInstalado();
        AvisoWindhawk.IsOpen = !cli;
        AvisoWindhawk.Message = VentanaBienvenida.RutaWindhawk() is null
            ? "Windhawk todavía no está instalado. Wind-Aether usa Windhawk para aplicar los mods."
            : "Tu Windhawk no permite que Aether Toolbox controle los mods (le falta windhawk-cli, que trae Windhawk 2.0). Pulsa Instalar para actualizarlo.";
        FilaPerfil.IsEnabled = cli;

        _cargando = true;
        string perfil = "ninguno";
        try
        {
            using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\AetherOS\Centro");
            perfil = k?.GetValue("PerfilWind") as string ?? "ninguno";
        }
        catch { /* sin ajuste */ }
        foreach (object o in CmbPerfil.Items)
        {
            if (o is ComboBoxItem it && (it.Tag as string) == perfil)
            {
                CmbPerfil.SelectedItem = it;
            }
        }
        List<ModWind> mods = Aether.ListaMods();
        ListaMods.ItemsSource = mods;
        _cargando = false;
        ActualizarConteo();
    }

    private void ActualizarConteo()
    {
        if (ListaMods.ItemsSource is not List<ModWind> mods)
        {
            return;
        }
        int activos = 0;
        foreach (ModWind m in mods)
        {
            if (m.Activo)
            {
                activos++;
            }
        }
        TxtConteo.Text = $"{activos} de {mods.Count} activos";
    }

    private async void Perfil_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando || CmbPerfil.SelectedItem is not ComboBoxItem it || it.Tag is not string perfil)
        {
            return;
        }
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Aplicando el perfil...";
        Estado.IsOpen = true;
        string r = (await Aether.Ejecutar($"Set-AetherPerfilWind -Perfil {perfil}")).Trim();
        _cargando = true;
        ListaMods.ItemsSource = Aether.ListaMods();
        _cargando = false;
        ActualizarConteo();
        Estado.Severity = r == "Perfil aplicado." ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        Estado.Message = string.IsNullOrEmpty(r) ? "Perfil aplicado." : r;
        Estado.IsOpen = true;
    }

    private async void Mod_Toggled(object sender, RoutedEventArgs e)
    {
        if (_cargando) return;
        if (sender is ToggleSwitch sw && sw.Tag is string id)
        {
            bool activo = sw.IsOn;
            await Aether.ActivarMod(id, activo);
            ActualizarConteo();
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
        string cli = (await Aether.Ejecutar("$c = Install-AetherWindhawkCli; if ($c) { 'CLI' } else { 'NO' }")).Trim();
        if (!cli.Contains("CLI"))
        {
            await Aether.Ejecutar("winget install --id RamenSoftware.Windhawk --exact --accept-package-agreements --accept-source-agreements --disable-interactivity");
        }
        else
        {
            // Mods de Wind-Aether importados (desactivados) para poder activarlos desde aquí.
            await Aether.Ejecutar("$m = Join-Path $script:AetherCarpeta 'Wind-Aether\\Wind-Aether-mods.json'; & (Get-AetherWindhawkCli) data import $m --yes");
        }
        AvisoWindhawk.IsOpen = !Aether.WindhawkInstalado();
        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = Aether.WindhawkInstalado() ? "Windhawk instalado." : "Revisa la instalacion de Windhawk.";
        Estado.IsOpen = true;
    }

    private void AbrirWindhawk_Click(object sender, RoutedEventArgs e)
    {
        if (!Herramientas.AbrirWindhawk())
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = "Windhawk no está instalado.";
            Estado.IsOpen = true;
        }
    }
}
