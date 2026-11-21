using CentroAether.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

/// <summary>Compatibility Database: estado de apps/funciones tras los ajustes de Aether.</summary>
public sealed partial class CompatibilidadPage : Page
{
    public CompatibilidadPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Contenedor.Children.Clear();
        foreach (EntradaCompat c in CompatibilidadServicio.Todas)
        {
            Contenedor.Children.Add(CrearTarjeta(c));
        }
    }

    private Border CrearTarjeta(EntradaCompat c)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icono = new FontIcon
        {
            Glyph = c.Ok ? "" : "",
            FontSize = 18,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 14, 0),
            Foreground = (Brush)Application.Current.Resources[
                c.Ok ? "SystemFillColorSuccessBrush" : "SystemFillColorCautionBrush"],
        };
        Grid.SetColumn(icono, 0);
        grid.Children.Add(icono);

        var textos = new StackPanel { Spacing = 2 };
        textos.Children.Add(new TextBlock { Text = c.Nombre, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        textos.Children.Add(new TextBlock
        {
            Text = c.Nota,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        Grid.SetColumn(textos, 1);
        grid.Children.Add(textos);

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
}
