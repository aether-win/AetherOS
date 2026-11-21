using System;
using System.Linq;
using System.Threading.Tasks;
using CentroAether.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

/// <summary>
/// Software Center: catálogo curado de apps confiables instalables con winget.
/// </summary>
public sealed partial class SoftwarePage : Page
{
    private bool _ocupado;

    public SoftwarePage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Construir();
    }

    private void Construir()
    {
        Contenedor.Children.Clear();
        foreach (string categoria in CatalogoApps.Todas.Select(a => a.Categoria).Distinct())
        {
            Contenedor.Children.Add(new TextBlock
            {
                Text = categoria,
                Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
                Margin = new Thickness(2, 8, 0, 2),
            });

            foreach (AppCatalogo app in CatalogoApps.Todas.Where(a => a.Categoria == categoria))
            {
                Contenedor.Children.Add(CrearTarjeta(app));
            }
        }
    }

    private Border CrearTarjeta(AppCatalogo app)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icono = new FontIcon
        {
            Glyph = app.Glyph,
            FontSize = 22,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0),
        };
        Grid.SetColumn(icono, 0);
        grid.Children.Add(icono);

        var textos = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        textos.Children.Add(new TextBlock { Text = app.Nombre, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        textos.Children.Add(new TextBlock
        {
            Text = app.Descripcion,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        Grid.SetColumn(textos, 1);
        grid.Children.Add(textos);

        var btn = new Button
        {
            Content = "Instalar",
            VerticalAlignment = VerticalAlignment.Center,
        };
        btn.Click += async (_, _) => await Instalar(app, btn);
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

    private async Task Instalar(AppCatalogo app, Button btn)
    {
        if (_ocupado)
        {
            return;
        }
        _ocupado = true;
        btn.IsEnabled = false;
        btn.Content = "Instalando…";
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = $"Instalando {app.Nombre}…";
        Estado.IsOpen = true;

        string salida = await Aether.InstalarWinget(app.WingetId);

        bool ok = !salida.Contains("ERROR", StringComparison.OrdinalIgnoreCase);
        btn.IsEnabled = true;
        btn.Content = ok ? "Listo" : "Reintentar";
        Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        Estado.Message = ok
            ? $"{app.Nombre} quedó instalado o ya estaba al día."
            : $"No se pudo instalar {app.Nombre}.";
        Estado.IsOpen = true;
        _ocupado = false;
    }

    private async void ActualizarTodo_Click(object sender, RoutedEventArgs e)
    {
        if (_ocupado)
        {
            return;
        }
        _ocupado = true;
        BtnActualizarTodo.IsEnabled = false;
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Buscando actualizaciones de todo lo instalado…";
        Estado.IsOpen = true;

        await Aether.ActualizarProgramas();

        BtnActualizarTodo.IsEnabled = true;
        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = "Listo. Se actualizó lo que tenía versión nueva.";
        Estado.IsOpen = true;
        _ocupado = false;
    }
}
