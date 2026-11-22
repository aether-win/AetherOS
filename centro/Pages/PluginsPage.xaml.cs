using System;
using System.Collections.Generic;
using System.Diagnostics;
using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;
using System.Windows.Media;

namespace CentroAether.Pages;

/// <summary>
/// Plugin System (v1): lista plugins declarados por manifiesto JSON. Cada plugin
/// solo puede abrir una URL o ruta (sin ejecución de comandos arbitrarios).
/// </summary>
public sealed partial class PluginsPage : PaginaBase
{
    public PluginsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        PluginsServicio.AsegurarCarpeta();
        TxtEjemplo.Text = "{ \"Nombre\": \"Mi plugin\", \"Descripcion\": \"Qué hace\", \"Abrir\": \"https://ejemplo.com\" }";
        Cargar();
    }

    private void Refrescar_Click(object sender, RoutedEventArgs e) => Cargar();

    private void Cargar()
    {
        Contenedor.Children.Clear();
        List<PluginInfo> plugins = PluginsServicio.Listar();
        Vacio.Visibility = plugins.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (PluginInfo p in plugins)
        {
            Contenedor.Children.Add(CrearTarjeta(p));
        }
    }

    private FilaAjuste CrearTarjeta(PluginInfo p)
    {
        Button? btn = null;
        if (!string.IsNullOrWhiteSpace(p.Abrir))
        {
            btn = new Button { Content = "Abrir" };
            btn.Click += (_, _) => Abrir(p);
        }

        return new FilaAjuste
        {
            Glyph = "\uEA86",
            Titulo = p.Nombre,
            Descripcion = p.Descripcion ?? "",
            Accion = btn,
        };
    }

    private void Abrir(PluginInfo p)
    {
        try
        {
            if (p.Abrir.Contains("://", StringComparison.Ordinal))
            {
                Sistema.Abrir(p.Abrir);
            }
            else
            {
                Process.Start(new ProcessStartInfo { FileName = p.Abrir, UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = $"No se pudo abrir «{p.Nombre}»: {ex.Message}";
            Estado.IsOpen = true;
        }
    }

    private void Carpeta_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            PluginsServicio.AsegurarCarpeta();
            Process.Start(new ProcessStartInfo { FileName = PluginsServicio.Carpeta, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = $"No se pudo abrir la carpeta: {ex.Message}";
            Estado.IsOpen = true;
        }
    }
}
