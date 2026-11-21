using System;
using System.Collections.Generic;
using System.Diagnostics;
using CentroAether.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

/// <summary>
/// Plugin System (v1): lista plugins declarados por manifiesto JSON. Cada plugin
/// solo puede abrir una URL o ruta (sin ejecución de comandos arbitrarios).
/// </summary>
public sealed partial class PluginsPage : Page
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

    private Border CrearTarjeta(PluginInfo p)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textos = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        textos.Children.Add(new TextBlock { Text = p.Nombre, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        if (!string.IsNullOrWhiteSpace(p.Descripcion))
        {
            textos.Children.Add(new TextBlock
            {
                Text = p.Descripcion,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }
        Grid.SetColumn(textos, 0);
        grid.Children.Add(textos);

        if (!string.IsNullOrWhiteSpace(p.Abrir))
        {
            var btn = new Button { Content = "Abrir", VerticalAlignment = VerticalAlignment.Center };
            btn.Click += async (_, _) => await Abrir(p);
            Grid.SetColumn(btn, 1);
            grid.Children.Add(btn);
        }

        return new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Child = grid,
        };
    }

    private async System.Threading.Tasks.Task Abrir(PluginInfo p)
    {
        try
        {
            if (p.Abrir.Contains("://", StringComparison.Ordinal))
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(p.Abrir));
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
