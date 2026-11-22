using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;
using System.Windows.Media;

namespace CentroAether.Pages;

/// <summary>
/// Game Library (base): detecta juegos de Steam y permite lanzarlos. Opcionalmente
/// activa el Modo Gaming y muestra el overlay al iniciar un juego.
/// </summary>
public sealed partial class BibliotecaPage : PaginaBase
{
    public BibliotecaPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Buscar();
    }

    private void Refrescar_Click(object sender, RoutedEventArgs e) => Buscar();

    private void Buscar()
    {
        Contenedor.Children.Clear();
        List<JuegoInfo> juegos = BibliotecaServicio.DetectarSteam();

        Vacio.Visibility = juegos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (JuegoInfo j in juegos)
        {
            Contenedor.Children.Add(CrearTarjeta(j));
        }
    }

    private Border CrearTarjeta(JuegoInfo j)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icono = new FontIcon
        {
            Glyph = "",
            FontSize = 20,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0),
        };
        Grid.SetColumn(icono, 0);
        grid.Children.Add(icono);

        var textos = new Pila { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        textos.Children.Add(new TextBlock { Text = j.Nombre, FontWeight = FontWeights.SemiBold });
        textos.Children.Add(new TextBlock
        {
            Text = j.Tienda,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
        });
        Grid.SetColumn(textos, 1);
        grid.Children.Add(textos);

        var btn = new Button
        {
            Content = "Jugar",
            Style = (Style)Application.Current.Resources["AccentButtonStyle"],
            VerticalAlignment = VerticalAlignment.Center,
        };
        btn.Click += async (_, _) => await Jugar(j);
        Grid.SetColumn(btn, 2);
        grid.Children.Add(btn);

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

    private async Task Jugar(JuegoInfo j)
    {
        if (ModoAlJugar.IsOn)
        {
            await Aether.CambiarModo("Gaming");
            VentanaOverlayModo.Mostrar("Gaming", j.Nombre);
        }

        try
        {
            Sistema.Abrir($"steam://rungameid/{j.AppId}");
            Estado.Severity = InfoBarSeverity.Success;
            Estado.Message = $"Iniciando {j.Nombre}…";
        }
        catch (Exception ex)
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = $"No se pudo iniciar {j.Nombre}: {ex.Message}";
        }
        Estado.IsOpen = true;
    }
}
