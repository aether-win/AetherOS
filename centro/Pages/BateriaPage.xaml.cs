using System;
using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;

namespace CentroAether.Pages;

public sealed partial class BateriaPage : PaginaBase
{
    private bool _cargando;
    private readonly System.Windows.Threading.DispatcherTimer _vivo = new() { Interval = TimeSpan.FromSeconds(2) };

    public BateriaPage()
    {
        this.InitializeComponent();
        _vivo.Tick += async (_, _) => await PintarVivo();
        foreach (int v in new[] { 10, 15, 20, 25, 30, 35, 40, 45, 50 })
        {
            CmbUmbral.Items.Add($"{v}%");
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _cargando = true;

        await PintarVivo();
        _vivo.Start();

        string auto = await Aether.LeerAjuste("BateriaAuto", "1");
        SwAuto.IsOn = auto.Trim() != "0";

        string umbral = (await Aether.LeerAjuste("UmbralBateria", "30")).Trim();
        int idx = CmbUmbral.Items.IndexOf($"{umbral}%");
        CmbUmbral.SelectedIndex = idx >= 0 ? idx : 4;

        string turbo = await Aether.LeerAjuste("LimitarTurbo", "0");
        SwTurbo.IsOn = turbo.Trim() == "1";

        _cargando = false;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _vivo.Stop();
    }

    private async System.Threading.Tasks.Task PintarVivo()
    {
        EstadoBateria b = await System.Threading.Tasks.Task.Run(BateriaVivo.Leer);
        if (!b.Hay)
        {
            TxtCarga.Text = "Esta PC no tiene batería.";
            BarraCarga.Visibility = Visibility.Collapsed;
            Vivo.Visibility = Visibility.Collapsed;
            return;
        }
        string estado = b.Cargando ? "cargando" : b.Conectado ? "conectada" : "con batería";
        TxtCarga.Text = $"{b.Porcentaje}% · {estado}";
        BarraCarga.Value = b.Porcentaje;
        Vivo.Poner(b.Cargando ? "Carga entrando" : "Consumo ahora", BateriaVivo.Tasa(b.TasaMw));
        Vivo.Poner(b.Cargando ? "Para llenarse" : "Tiempo restante", BateriaVivo.Tiempo(b.MinutosRestantes));
        Vivo.Poner("Energía guardada", b.RestanteMwh > 0 ? $"{b.RestanteMwh / 1000.0:0.0} Wh" : "—");
        Vivo.Poner("Voltaje", b.VoltajeMv > 0 ? $"{b.VoltajeMv / 1000.0:0.00} V" : "—");
        Vivo.Poner("Salud", b.SaludPct > 0 ? $"{b.SaludPct}%" : "—");
        Vivo.Poner("Ciclos de carga", b.Ciclos >= 0 ? b.Ciclos.ToString() : "—");
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
