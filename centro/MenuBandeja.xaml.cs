using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CentroAether.Controles;
using CentroAether.Services;

namespace CentroAether;

/// <summary>
/// Menú del ícono de la bandeja, oscuro y con el diseño de Aether (el menú normal de
/// Windows Forms sale blanco). Dos grupos: modo de uso y mando; más "Abrir Centro".
/// Se cierra solo al hacer clic fuera.
/// </summary>
public partial class MenuBandeja : Window
{
    public MenuBandeja()
    {
        InitializeComponent();
        Deactivated += (_, _) => Hide();
        Tema.BarraOscura(this);
    }

    /// <summary>Muestra el menú junto al cursor (en píxeles de pantalla), dentro del área de trabajo.</summary>
    public void MostrarEn(System.Drawing.Point cursor, System.Drawing.Rectangle area)
    {
        Construir();
        Left = -10000;
        Top = -10000;
        Show();
        UpdateLayout();

        // Convertir píxeles de pantalla a unidades de WPF según el DPI de este monitor.
        Matrix aWpf = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        Point c = aWpf.Transform(new Point(cursor.X, cursor.Y));
        Point min = aWpf.Transform(new Point(area.Left, area.Top));
        Point max = aWpf.Transform(new Point(area.Right, area.Bottom));

        double x = c.X - ActualWidth + 18;    // el margen de la sombra
        double y = c.Y - ActualHeight + 18;
        Left = Math.Max(min.X, Math.Min(x, max.X - ActualWidth));
        Top = Math.Max(min.Y, Math.Min(y, max.Y - ActualHeight));

        Activate();
    }

    private void Construir()
    {
        Contenido.Children.Clear();
        string modo = Aether.ModoGuardado();

        // Encabezado
        var cabecera = new Grid { Margin = new Thickness(8, 6, 8, 10) };
        cabecera.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        cabecera.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Border logo = Ui.CajaIcono(Aether.Disponible ? Ui.GlyphModo(modo) : "",
            Aether.Disponible ? Ui.ColorModo(modo) : Ui.Hex("#2F8CFF"), 40, 18);
        logo.Margin = new Thickness(0, 0, 12, 0);
        cabecera.Children.Add(logo);
        var textos = new Pila { VerticalAlignment = VerticalAlignment.Center };
        textos.Children.Add(new TextBlock { Text = "Aether OS", FontSize = 15, FontWeight = FontWeights.SemiBold });
        textos.Children.Add(new TextBlock
        {
            Text = Aether.Disponible ? $"Modo {Ui.NombreModo(modo)}" : "Aether OS no instalado",
            FontSize = 12.5,
            Foreground = Ui.Recurso("TextFillColorSecondaryBrush"),
        });
        Grid.SetColumn(textos, 1);
        cabecera.Children.Add(textos);
        Contenido.Children.Add(cabecera);

        // Modo de uso
        Contenido.Children.Add(Titulo("MODO DE USO"));
        foreach ((string id, string nombre) in new[] { ("Trabajo", "Trabajo"), ("Gaming", "Gaming"), ("Bateria", "Batería") })
        {
            bool activo = Aether.Disponible && Ui.NombreModo(modo) == Ui.NombreModo(id);
            string destino = id;
            Contenido.Children.Add(Opcion(Ui.GlyphModo(id), Ui.ColorModo(id), $"Modo {nombre}", activo,
                () => _ = SegundoPlano.CambiarModo(destino)));
        }

        Contenido.Children.Add(Separador());

        // Mando
        Contenido.Children.Add(Titulo("MANDO"));
        bool emulando = MotorControlador.Activo;
        Contenido.Children.Add(Opcion("", Ui.Hex("#6F87A3"), "Apagado (mando normal)", !emulando,
            () => SegundoPlano.CambiarMando(null)));
        Contenido.Children.Add(Opcion("", Ui.Hex("#3FBF7F"), "Mando de Xbox 360",
            emulando && MotorControlador.Objetivo == TipoVirtual.Xbox360,
            () => SegundoPlano.CambiarMando(TipoVirtual.Xbox360)));
        Contenido.Children.Add(Opcion("", Ui.Hex("#2F8CFF"), "Mando de PS4 (DualShock 4)",
            emulando && MotorControlador.Objetivo == TipoVirtual.DualShock4,
            () => SegundoPlano.CambiarMando(TipoVirtual.DualShock4)));

        Contenido.Children.Add(Separador());

        Contenido.Children.Add(Opcion("\uE80F", Ui.Hex("#4FC3F7"), "Abrir Aether Toolbox", false,
            () => ((App)Application.Current).MostrarCentro()));

        Contenido.Children.Add(Separador());

        // Cerrar TODO: ventana, bandeja, atajos y mando emulado (p. ej. para borrar o actualizar
        // la carpeta del programa). Se vuelve a abrir con su acceso directo o al iniciar sesión.
        Contenido.Children.Add(Opcion("\uE7E8", Ui.Hex("#F0565C"), "Cerrar Aether Toolbox", false,
            () => Application.Current.Shutdown()));
    }

    private static TextBlock Titulo(string texto) => new()
    {
        Text = texto,
        FontSize = 11,
        FontWeight = FontWeights.SemiBold,
        Foreground = Ui.Recurso("TextFillColorTertiaryBrush"),
        Margin = new Thickness(10, 6, 0, 4),
    };

    private static Border Separador() => new()
    {
        Height = 1,
        Background = new SolidColorBrush(Ui.Hex("#1E3A5C")),
        Margin = new Thickness(8, 6, 8, 4),
    };

    private Button Opcion(string glyph, Color color, string texto, bool activo, Action accion)
    {
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var caja = new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(activo ? (byte)255 : (byte)45, color.R, color.G, color.B)),
            Margin = new Thickness(0, 0, 12, 0),
            Child = new FontIcon
            {
                Glyph = glyph,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = activo ? Brushes.White : new SolidColorBrush(color),
            },
        };
        g.Children.Add(caja);

        var etiqueta = new TextBlock
        {
            Text = texto,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = activo ? FontWeights.SemiBold : FontWeights.Normal,
        };
        Grid.SetColumn(etiqueta, 1);
        g.Children.Add(etiqueta);

        if (activo)
        {
            var check = new FontIcon { Glyph = "", FontSize = 13, Foreground = new SolidColorBrush(color) };
            Grid.SetColumn(check, 2);
            g.Children.Add(check);
        }

        var b = new Button { Content = g, Style = (Style)FindResource("ItemBandeja") };
        b.Click += (_, _) =>
        {
            Hide();
            accion();
        };
        return b;
    }
}
