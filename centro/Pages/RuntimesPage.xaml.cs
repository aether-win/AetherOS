using System.Threading.Tasks;
using CentroAether.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

/// <summary>
/// Runtime Manager: detecta VC++, .NET, DirectX, WebView2 y Python, y permite
/// instalar los que falten usando los paquetes oficiales de Microsoft (winget).
/// </summary>
public sealed partial class RuntimesPage : Page
{
    private bool _ocupado;

    public RuntimesPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Cargar();
    }

    private void Refrescar_Click(object sender, RoutedEventArgs e) => Cargar();

    private void Cargar()
    {
        Contenedor.Children.Clear();
        foreach (RuntimeInfo rt in RuntimesServicio.Detectar())
        {
            Contenedor.Children.Add(CrearTarjeta(rt));
        }
    }

    private Border CrearTarjeta(RuntimeInfo rt)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textos = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        textos.Children.Add(new TextBlock
        {
            Text = rt.Nombre,
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
        });
        textos.Children.Add(new TextBlock
        {
            Text = rt.Descripcion,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        Grid.SetColumn(textos, 0);
        grid.Children.Add(textos);

        var accion = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };

        if (rt.Instalado)
        {
            accion.Children.Add(new FontIcon
            {
                Glyph = "",
                FontSize = 16,
                Foreground = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"],
            });
            accion.Children.Add(new TextBlock
            {
                Text = rt.Version == "instalado" ? "Instalado" : $"v{rt.Version}",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }
        else
        {
            var btn = new Button { Content = "Instalar" };
            btn.Click += async (_, _) => await Instalar(rt, btn);
            accion.Children.Add(btn);
        }

        Grid.SetColumn(accion, 1);
        grid.Children.Add(accion);

        return new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(18, 14, 18, 14),
            Child = grid,
        };
    }

    private async Task Instalar(RuntimeInfo rt, Button btn)
    {
        if (_ocupado)
        {
            return;
        }
        _ocupado = true;
        btn.IsEnabled = false;
        btn.Content = "Instalando…";
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = $"Instalando {rt.Nombre}… esto puede tardar un momento.";
        Estado.IsOpen = true;

        string salida = await Aether.InstalarWinget(rt.WingetId);

        _ocupado = false;
        bool ok = !salida.Contains("ERROR", System.StringComparison.OrdinalIgnoreCase);
        Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        Estado.Message = ok
            ? $"{rt.Nombre} quedó instalado o ya estaba al día."
            : $"No se pudo instalar {rt.Nombre}. Puedes reintentar.";
        Estado.IsOpen = true;

        Cargar();
    }
}
