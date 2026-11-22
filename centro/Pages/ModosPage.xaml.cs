using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CentroAether.Controles;
using CentroAether.Services;

namespace CentroAether.Pages;

public sealed partial class ModosPage : PaginaBase
{
    private string _modoActual = "";
    private bool _ocupado;

    private static readonly (string id, string titulo, string desc, string[] puntos)[] Modos =
    {
        ("Trabajo", "Trabajo", "Equilibrado para el día a día.",
            new[] { "Todo activo y notificaciones", "Animaciones completas de Windows 11", "Turbo del procesador disponible" }),
        ("Gaming", "Gaming", "Máximo rendimiento para jugar.",
            new[] { "Solo con el cargador conectado", "Sin estacionar núcleos ni segundo plano", "No molestar y mods pesados en pausa" }),
        ("Bateria", "Batería", "Eficiente para durar más.",
            new[] { "Ahorro de batería de Windows", "Procesador eficiente, sin turbo", "Wind-Aether en versión ligera" }),
    };

    public ModosPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Construir();
        await RefrescarModo();
    }

    private async Task RefrescarModo()
    {
        if (!Aether.Disponible)
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = "No se encontró Aether.Centro.ps1 en este equipo.";
            Estado.IsOpen = true;
            return;
        }
        _modoActual = await Aether.ModoActual();
        Construir();
    }

    private void Construir()
    {
        GridModos.Children.Clear();
        foreach (var m in Modos)
        {
            GridModos.Children.Add(CrearTarjeta(m.id, m.titulo, m.desc, m.puntos));
        }
    }

    private Button CrearTarjeta(string id, string titulo, string desc, string[] puntos)
    {
        bool activo = string.Equals(Ui.NombreModo(_modoActual), Ui.NombreModo(id), StringComparison.OrdinalIgnoreCase);
        Color color = Ui.ColorModo(id);

        var cabecera = new Grid();
        cabecera.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        cabecera.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cabecera.Children.Add(Ui.CajaIcono(Ui.GlyphModo(id), color, 52, 24));
        if (activo)
        {
            Border insignia = Ui.Insignia("Activo", color);
            insignia.HorizontalAlignment = HorizontalAlignment.Right;
            insignia.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(insignia, 1);
            cabecera.Children.Add(insignia);
        }

        var contenido = new Pila { Spacing = 12 };
        contenido.Children.Add(cabecera);
        var textos = new Pila { Spacing = 2 };
        textos.Children.Add(new TextBlock { Text = titulo, FontSize = 20, FontWeight = FontWeights.SemiBold });
        textos.Children.Add(new TextBlock { Text = desc, FontSize = 13, TextWrapping = TextWrapping.Wrap, Foreground = Ui.Recurso("TextFillColorSecondaryBrush") });
        contenido.Children.Add(textos);

        var lista = new Pila { Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };
        foreach (string p in puntos)
        {
            var fila = new Pila { Orientation = Orientation.Horizontal, Spacing = 8 };
            fila.Children.Add(new FontIcon { Glyph = "", FontSize = 12, Foreground = new SolidColorBrush(color) });
            fila.Children.Add(new TextBlock { Text = p, FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Foreground = Ui.Recurso("TextFillColorSecondaryBrush") });
            lista.Children.Add(fila);
        }
        contenido.Children.Add(lista);
        contenido.Children.Add(new TextBlock
        {
            Text = activo ? "Este es tu modo actual" : "Clic para activar",
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0),
            Foreground = activo ? new SolidColorBrush(color) : Ui.Recurso("AccentTextFillColorPrimaryBrush"),
        });

        var b = new Button
        {
            Content = contenido,
            Style = (Style)FindResource("MosaicoStyle"),
            Padding = new Thickness(20, 20, 20, 18),
        };
        if (activo)
        {
            b.BorderBrush = new SolidColorBrush(color);
            b.BorderThickness = new Thickness(2);
        }
        b.Click += async (_, _) => await Aplicar(id);
        return b;
    }

    private async Task Aplicar(string modo)
    {
        if (_ocupado)
        {
            return;
        }
        // "Cargando modo": se bloquean las tarjetas y se avisa hasta que quede estable.
        _ocupado = true;
        GridModos.IsEnabled = false;
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = $"Cargando modo {Ui.NombreModo(modo)}…";
        Estado.IsOpen = true;

        string salida = await Aether.CambiarModo(modo);

        await RefrescarModo();
        GridModos.IsEnabled = true;
        _ocupado = false;

        if (salida.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            Estado.Severity = InfoBarSeverity.Error;
            Estado.Message = "No se pudo cambiar de modo.";
        }
        else
        {
            Estado.Severity = InfoBarSeverity.Success;
            Estado.Message = $"Modo {Ui.NombreModo(modo)} activo.";
            // Cada modo muestra su propio overlay (Gaming, Trabajo o Batería).
            VentanaOverlayModo.Mostrar(modo);
        }
        Estado.IsOpen = true;
        RefrescarModoMenu();
    }

    private void VistaPreviaOverlay_Click(object sender, RoutedEventArgs e)
        => VentanaOverlayModo.Mostrar(string.IsNullOrEmpty(_modoActual) ? "Trabajo" : _modoActual);
}
