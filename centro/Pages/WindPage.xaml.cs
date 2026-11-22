using System;
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
        IniciarVigiaProgreso();
        AvisoWindhawk.IsOpen = !Aether.WindhawkInstalado();

        _cargando = true;
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

    private async void Mod_Toggled(object sender, RoutedEventArgs e)
    {
        if (_cargando) return;
        if (sender is ToggleSwitch sw && sw.Tag is string id)
        {
            bool activo = sw.IsOn;
            sw.IsEnabled = false;
            Estado.Severity = InfoBarSeverity.Informational;
            Estado.Message = activo ? "Activando el mod en Windhawk…" : "Desactivando el mod…";
            Estado.IsOpen = true;
            bool ok = (await Aether.ActivarMod(id, activo)).Contains("True", StringComparison.OrdinalIgnoreCase);
            sw.IsEnabled = true;
            if (!ok)
            {
                // Windhawk no confirmó el cambio: el interruptor vuelve a como estaba.
                _cargando = true;
                sw.IsOn = !activo;
                _cargando = false;
            }
            ActualizarConteo();
            Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Error;
            Estado.Message = ok
                ? (activo ? "Mod activado en Windhawk." : "Mod desactivado.")
                : "Windhawk no pudo aplicar el mod. Detalle en C:\\ProgramData\\Aether\\autoreparacion.log";
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
        await Aether.Ejecutar("[void](Install-AetherWindMods)");
        AvisoWindhawk.IsOpen = !Aether.WindhawkInstalado();
        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = Aether.WindhawkInstalado() ? "Windhawk instalado." : "Revisa la instalacion de Windhawk.";
        Estado.IsOpen = true;
    }

    // Progreso de Windhawk (instalando/compilando mods en segundo plano).
    private readonly System.Windows.Threading.DispatcherTimer _vigiaProgreso = new() { Interval = TimeSpan.FromSeconds(2) };
    private bool _vigiaLista;

    private void RevisarProgreso()
    {
        string archivo = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Aether", "windhawk-progreso.txt");
        string texto = "";
        try { if (System.IO.File.Exists(archivo)) { texto = System.IO.File.ReadAllText(archivo).Trim(); } } catch { /* en uso */ }
        bool antes = Progreso.IsOpen;
        Progreso.IsOpen = texto.Length > 0;
        if (texto.Length > 0)
        {
            Progreso.Message = texto + " — puede tardar unos minutos; los interruptores esperan a que termine.";
        }
        else if (antes)
        {
            _cargando = true;
            ListaMods.ItemsSource = Aether.ListaMods();
            _cargando = false;
            ActualizarConteo();
        }
    }

    private void IniciarVigiaProgreso()
    {
        if (!_vigiaLista)
        {
            _vigiaProgreso.Tick += (_, _) => RevisarProgreso();
            Unloaded += (_, _) => _vigiaProgreso.Stop();
            Loaded += (_, _) => _vigiaProgreso.Start();
            _vigiaLista = true;
        }
        _vigiaProgreso.Start();
        RevisarProgreso();
    }

    private void Registro_Click(object sender, RoutedEventArgs e)
    {
        string log = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Aether", "autoreparacion.log");
        if (!System.IO.File.Exists(log))
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(log)!);
            System.IO.File.WriteAllText(log, "Registro de Aether (Windhawk y drivers). Todavía no hay entradas." + Environment.NewLine);
        }
        // Se abre sin privilegios con el Bloc de notas.
        System.Diagnostics.Process.Start("explorer.exe", "\"" + log + "\"");
    }
}
