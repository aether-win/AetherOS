using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace CentroAether.Controles;

/// <summary>
/// Utilidades de diseño compartidas por todas las secciones: colores de los modos,
/// insignias, títulos de sección, mosaicos y el ícono en caja con degradado. Así todas
/// las páginas se ven igual de ordenadas sin repetir código.
/// </summary>
public static class Ui
{
    public static Brush Recurso(string clave) =>
        Application.Current?.TryFindResource(clave) as Brush ?? Brushes.Gray;

    public static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    /// <summary>Color propio de cada modo (el mismo del overlay).</summary>
    public static Color ColorModo(string? modo) => (modo ?? "").Trim().ToLowerInvariant() switch
    {
        "gaming" => Hex("#2F8CFF"),
        "bateria" or "batería" => Hex("#2FA86A"),
        _ => Hex("#7A5AF8"),   // Trabajo
    };

    public static string GlyphModo(string? modo) => (modo ?? "").Trim().ToLowerInvariant() switch
    {
        "gaming" => "",
        "bateria" or "batería" => "",
        _ => "",
    };

    public static string NombreModo(string? modo) => (modo ?? "").Trim().ToLowerInvariant() switch
    {
        "gaming" => "Gaming",
        "bateria" or "batería" => "Batería",
        "" => "—",
        _ => "Trabajo",
    };

    public static LinearGradientBrush Degradado(Color a, Color b) =>
        new(a, b, new Point(0, 0), new Point(1, 1));

    /// <summary>Caja redondeada con degradado y un ícono blanco al centro.</summary>
    public static Border CajaIcono(string glyph, Color color, double tam = 40, double tamIcono = 18)
    {
        Color claro = Color.FromArgb(255,
            (byte)Math.Min(255, color.R + 45), (byte)Math.Min(255, color.G + 45), (byte)Math.Min(255, color.B + 45));
        return new Border
        {
            Width = tam,
            Height = tam,
            CornerRadius = new CornerRadius(tam * 0.28),
            Background = Degradado(claro, color),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new FontIcon { Glyph = glyph, FontSize = tamIcono, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center },
        };
    }

    /// <summary>Etiqueta redondeada pequeña ("Activo", "Instalado", "v1.2"...).</summary>
    public static Border Insignia(string texto, Color color)
    {
        return new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 3, 10, 4),
            Background = new SolidColorBrush(Color.FromArgb(48, color.R, color.G, color.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(110, color.R, color.G, color.B)),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = texto,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(255,
                    (byte)Math.Min(255, color.R + 70), (byte)Math.Min(255, color.G + 70), (byte)Math.Min(255, color.B + 70))),
            },
        };
    }

    /// <summary>Título pequeño que separa grupos dentro de una sección.</summary>
    public static TextBlock Seccion(string texto) => new()
    {
        Text = texto,
        Style = Application.Current?.TryFindResource("SeccionTextBlockStyle") as Style,
    };

    /// <summary>Mosaico grande clicable: ícono con color, título y descripción corta.</summary>
    public static Button Mosaico(string glyph, string titulo, string descripcion, Color color, Action alClic)
    {
        var contenido = new Pila { Spacing = 10 };
        contenido.Children.Add(new Border { HorizontalAlignment = HorizontalAlignment.Left, Child = CajaIcono(glyph, color, 40, 18) });
        var textos = new Pila { Spacing = 2 };
        textos.Children.Add(new TextBlock { Text = titulo, FontSize = 15, FontWeight = FontWeights.SemiBold });
        textos.Children.Add(new TextBlock
        {
            Text = descripcion,
            FontSize = 12.5,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Recurso("TextFillColorSecondaryBrush"),
        });
        contenido.Children.Add(textos);

        var b = new Button
        {
            Content = contenido,
            Style = Application.Current?.TryFindResource("MosaicoStyle") as Style,
        };
        b.Click += (_, _) => alClic();
        return b;
    }
}

/// <summary>
/// Encabezado común de cada sección: ícono en caja con degradado, título, subtítulo y,
/// a la derecha, un contenido opcional (botón "Actualizar", insignia del modo, etc.).
/// En XAML: &lt;c:Encabezado Glyph="&amp;#xE7FC;" Titulo="Modos" Subtitulo="..."&gt;(acción)&lt;/c:Encabezado&gt;
/// </summary>
[ContentProperty(nameof(Accion))]
public class Encabezado : Grid
{
    private readonly ContentControl _icono = new() { Focusable = false, Margin = new Thickness(0, 0, 16, 0) };
    private readonly TextBlock _titulo = new() { FontSize = 26, FontWeight = FontWeights.SemiBold };
    private readonly TextBlock _subtitulo = new() { FontSize = 14, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) };
    private readonly ContentControl _accion = new() { Focusable = false, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0) };

    private string _glyph = "";
    private string _color = "#2F8CFF";
    private object? _accionContenido;

    public Encabezado()
    {
        Margin = new Thickness(0, 0, 0, 8);
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _subtitulo.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        var textos = new Pila { VerticalAlignment = VerticalAlignment.Center };
        textos.Children.Add(_titulo);
        textos.Children.Add(_subtitulo);

        SetColumn(_icono, 0);
        SetColumn(textos, 1);
        SetColumn(_accion, 2);
        Children.Add(_icono);
        Children.Add(textos);
        Children.Add(_accion);
        PintarIcono();
    }

    public string Glyph { get => _glyph; set { _glyph = value; PintarIcono(); } }

    /// <summary>Color del ícono en formato #RRGGBB (por defecto el azul Aether).</summary>
    public string ColorIcono { get => _color; set { _color = value; PintarIcono(); } }

    public string Titulo { get => _titulo.Text; set => _titulo.Text = value; }

    public string Subtitulo
    {
        get => _subtitulo.Text;
        set
        {
            _subtitulo.Text = value;
            _subtitulo.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public object? Accion
    {
        get => _accionContenido;
        set { _accionContenido = value; _accion.Content = value; }
    }

    private void PintarIcono() => _icono.Content = Ui.CajaIcono(_glyph, Ui.Hex(_color), 52, 24);
}

/// <summary>
/// Fila de ajuste al estilo de Configuración de Windows 11: ícono a la izquierda, título y
/// descripción en el centro y el control (botón, interruptor, lista) a la derecha. Lo que va
/// en "Debajo" ocupa una segunda línea a lo ancho (deslizadores, cuadros de texto…).
/// </summary>
[ContentProperty(nameof(Accion))]
public class FilaAjuste : Border
{
    private readonly ContentControl _icono = new() { Focusable = false, Margin = new Thickness(0, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _titulo = new() { FontSize = 14.5, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _descripcion = new() { FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) };
    private readonly TextBlock _nota = new() { FontSize = 12.5, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) };
    private readonly ContentControl _accion = new() { Focusable = false, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0) };
    private readonly ContentControl _debajo = new() { Focusable = false, Margin = new Thickness(0, 14, 0, 0) };

    private string _glyph = "";
    private Brush? _colorIcono;
    private object? _accionContenido;
    private object? _debajoContenido;

    public FilaAjuste()
    {
        SetResourceReference(StyleProperty, "FilaAjusteStyle");
        _descripcion.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        _nota.Visibility = Visibility.Collapsed;
        _descripcion.Visibility = Visibility.Collapsed;
        _debajo.Visibility = Visibility.Collapsed;

        var textos = new Pila { VerticalAlignment = VerticalAlignment.Center };
        textos.Children.Add(_titulo);
        textos.Children.Add(_descripcion);
        textos.Children.Add(_nota);

        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetColumn(_icono, 0);
        Grid.SetColumn(textos, 1);
        Grid.SetColumn(_accion, 2);
        Grid.SetRow(_debajo, 1);
        Grid.SetColumn(_debajo, 1);
        Grid.SetColumnSpan(_debajo, 2);
        g.Children.Add(_icono);
        g.Children.Add(textos);
        g.Children.Add(_accion);
        g.Children.Add(_debajo);
        Child = g;
        PintarIcono();
    }

    public string Glyph { get => _glyph; set { _glyph = value; PintarIcono(); } }

    /// <summary>Color del ícono (por defecto celeste Aether).</summary>
    public Brush? ColorIcono { get => _colorIcono; set { _colorIcono = value; PintarIcono(); } }

    public string Titulo { get => _titulo.Text; set => _titulo.Text = value; }

    public string Descripcion
    {
        get => _descripcion.Text;
        set
        {
            _descripcion.Text = value;
            _descripcion.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>Tercera línea opcional, destacada (p. ej. "Conectado" en verde).</summary>
    public string Nota
    {
        get => _nota.Text;
        set
        {
            _nota.Text = value;
            _nota.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public Brush? ColorNota { get => _nota.Foreground; set => _nota.Foreground = value; }

    public object? Accion
    {
        get => _accionContenido;
        set
        {
            _accionContenido = value;
            _accion.Content = value;
            _accion.Visibility = value is null ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public object? Debajo
    {
        get => _debajoContenido;
        set
        {
            _debajoContenido = value;
            _debajo.Content = value;
            _debajo.Visibility = value is null ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void PintarIcono()
    {
        if (string.IsNullOrEmpty(_glyph))
        {
            _icono.Visibility = Visibility.Collapsed;
            return;
        }
        _icono.Visibility = Visibility.Visible;
        var icono = new FontIcon { Glyph = _glyph, FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center };
        if (_colorIcono is not null)
        {
            icono.Foreground = _colorIcono;
        }
        else
        {
            icono.SetResourceReference(TextBlock.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
        }
        _icono.Content = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(10),
            Background = Ui.Recurso("IconoFondoBrush"),
            Child = icono,
        };
    }
}
