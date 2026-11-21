using System;
using System.Collections.Generic;
using CentroAether.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

/// <summary>
/// Aether Controller (base): detecta mandos conectados vía Windows.Gaming.Input.
/// El motor de mapeo (emulación Xbox 360, perfiles) basado en DS4Windows llegará después.
/// </summary>
public sealed partial class ControllerPage : Page
{
    private bool _ocupado;

    public ControllerPage()
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
        List<MandoInfo> mandos = MandosServicio.Detectar();
        Vacio.Visibility = mandos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (MandoInfo m in mandos)
        {
            Contenedor.Children.Add(CrearTarjeta(m));
        }
    }

    private Border CrearTarjeta(MandoInfo m)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        sp.Children.Add(new FontIcon
        {
            Glyph = "",
            FontSize = 20,
            VerticalAlignment = VerticalAlignment.Center,
        });

        var textos = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        textos.Children.Add(new TextBlock { Text = m.Nombre, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        textos.Children.Add(new TextBlock
        {
            Text = m.EsGamepad ? "Conectado · compatible" : "Conectado",
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"],
        });
        sp.Children.Add(textos);

        return new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Child = sp,
        };
    }

    private async void Ds4_Click(object sender, RoutedEventArgs e)
    {
        if (_ocupado)
        {
            return;
        }
        _ocupado = true;
        BtnDs4.IsEnabled = false;
        BtnDs4.Content = "Instalando…";
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Instalando DS4Windows…";
        Estado.IsOpen = true;

        string salida = await Aether.InstalarWinget("Ryochan7.DS4Windows");

        bool ok = !salida.Contains("ERROR", StringComparison.OrdinalIgnoreCase);
        BtnDs4.IsEnabled = true;
        BtnDs4.Content = ok ? "Listo" : "Reintentar";
        Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        Estado.Message = ok ? "DS4Windows instalado o ya estaba al día." : "No se pudo instalar DS4Windows.";
        Estado.IsOpen = true;
        _ocupado = false;
    }
}
