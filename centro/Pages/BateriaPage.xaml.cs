using System;
using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;

namespace CentroAether.Pages;

public sealed partial class BateriaPage : PaginaBase
{
    private bool _cargando;

    public BateriaPage()
    {
        this.InitializeComponent();
        foreach (int v in new[] { 10, 15, 20, 25, 30, 35, 40, 45, 50 })
        {
            CmbUmbral.Items.Add($"{v}%");
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _cargando = true;

        string carga = await Aether.Bateria();
        TxtCarga.Text = carga == "SIN" ? "Esta PC no tiene bateria." : $"Carga actual: {carga}%";

        string auto = await Aether.LeerAjuste("BateriaAuto", "1");
        SwAuto.IsOn = auto.Trim() != "0";

        string umbral = (await Aether.LeerAjuste("UmbralBateria", "30")).Trim();
        int idx = CmbUmbral.Items.IndexOf($"{umbral}%");
        CmbUmbral.SelectedIndex = idx >= 0 ? idx : 4;

        string turbo = await Aether.LeerAjuste("LimitarTurbo", "0");
        SwTurbo.IsOn = turbo.Trim() == "1";

        _cargando = false;
    }

    private async void Auto_Toggled(object sender, RoutedEventArgs e)
    {
        if (_cargando) return;
        await Aether.GuardarAjuste("BateriaAuto", SwAuto.IsOn ? 1 : 0);
        Aviso(SwAuto.IsOn ? "Modo Bateria automatico activado." : "Modo Bateria automatico desactivado.");
    }

    private async void Umbral_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando || CmbUmbral.SelectedItem is not string s) return;
        if (int.TryParse(s.Replace("%", ""), out int v))
        {
            await Aether.GuardarAjuste("UmbralBateria", v);
            Aviso($"El Modo Bateria se activara al {v}%.");
        }
    }

    private async void Turbo_Toggled(object sender, RoutedEventArgs e)
    {
        if (_cargando) return;
        await Aether.GuardarAjuste("LimitarTurbo", SwTurbo.IsOn ? 1 : 0);
        Aviso("Preferencia guardada.");
    }

    private void Aviso(string texto)
    {
        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = texto;
        Estado.IsOpen = true;
    }
}
