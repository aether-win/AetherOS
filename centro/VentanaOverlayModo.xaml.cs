using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CentroAether;

/// <summary>
/// Aviso animado que aparece al activar un modo (Gaming, Trabajo o Batería), centrado en
/// la pantalla. Cada modo tiene su color, ícono y texto. Entra con zoom suave y fundido,
/// una barra se llena mientras está visible y sale solo a los 3 segundos. Siempre al
/// frente, sin robar el foco (no interrumpe juegos) y fuera de la barra de tareas.
/// Si se cambia de modo otra vez, el aviso anterior se cierra para no amontonarse.
/// </summary>
public partial class VentanaOverlayModo : Window
{
    private static readonly TimeSpan Duracion = TimeSpan.FromSeconds(3);
    private static VentanaOverlayModo? _actual;

    private readonly DispatcherTimer _autoCierre = new() { Interval = Duracion };
    private bool _cerrando;

    public VentanaOverlayModo(string modo, string? detalle = null)
    {
        InitializeComponent();
        Aplicar(modo, detalle);
        Posicionar();

        _autoCierre.Tick += (_, _) => Salir();
        Loaded += (_, _) => Entrar();
    }

    private void Aplicar(string modo, string? detalle)
    {
        (string titulo, string sub, string glyph, Color color) = modo.Trim().ToLowerInvariant() switch
        {
            "gaming" => ("Modo Gaming", "Máximo rendimiento activado", "", Hex("#2F8CFF")),
            "trabajo" => ("Modo Trabajo", "Equilibrado para el día a día", "", Hex("#7A5AF8")),
            "bateria" or "batería" => ("Modo Batería", "Optimizado para durar más", "", Hex("#2FA86A")),
            _ => ($"Modo {modo}", "", "", Hex("#2F8CFF")),
        };

        TxtTitulo.Text = titulo;
        TxtSub.Text = string.IsNullOrWhiteSpace(detalle) ? sub : detalle!;
        Icono.Glyph = glyph;

        Color claro = Aclarar(color, 0.35);
        Color oscuro = Mezclar(Hex("#0B1426"), color, 0.22);

        // Tarjeta: degradado oscuro teñido con el color del modo y borde luminoso.
        Tarjeta.Background = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(0xF5, oscuro.R, oscuro.G, oscuro.B), 0),
                new GradientStop(Color.FromArgb(0xF5, 0x0B, 0x14, 0x26), 1),
            }, new Point(0, 0), new Point(1, 1));
        Tarjeta.BorderBrush = new LinearGradientBrush(
            Color.FromArgb(0xCC, claro.R, claro.G, claro.B),
            Color.FromArgb(0x40, color.R, color.G, color.B), new Point(0, 0), new Point(1, 1));

        Resplandor.Background = new SolidColorBrush(color);
        Brillo.Fill = new SolidColorBrush(color);
        CajaIcono.Background = new LinearGradientBrush(claro, color, new Point(0, 0), new Point(1, 1));
        Barra.Background = new LinearGradientBrush(color, claro, new Point(0, 0), new Point(1, 0));
        TxtMarca.Foreground = new SolidColorBrush(claro);
    }

    /// <summary>Centrado en la pantalla principal (área de trabajo, sin la barra de tareas).</summary>
    private void Posicionar()
    {
        Rect area = SystemParameters.WorkArea;
        Left = area.Left + ((area.Width - Width) / 2);
        Top = area.Top + ((area.Height - Height) / 2);
    }

    private void Entrar()
    {
        var suave = new CubicEase { EasingMode = EasingMode.EaseOut };
        var rebote = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 };
        Raiz.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = suave });
        Escala.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.9, 1, TimeSpan.FromMilliseconds(420)) { EasingFunction = rebote });
        Escala.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.9, 1, TimeSpan.FromMilliseconds(420)) { EasingFunction = rebote });

        // La barra se llena durante el tiempo que el aviso está visible.
        double ancho = ((FrameworkElement)Barra.Parent).ActualWidth;
        Barra.BeginAnimation(WidthProperty, new DoubleAnimation(0, ancho > 0 ? ancho : 340, Duracion)
        {
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        });

        _autoCierre.Start();
    }

    private void Salir()
    {
        if (_cerrando)
        {
            return;
        }
        _cerrando = true;
        _autoCierre.Stop();

        var suave = new CubicEase { EasingMode = EasingMode.EaseIn };
        var fuera = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)) { EasingFunction = suave };
        fuera.Completed += (_, _) => Close();
        Raiz.BeginAnimation(OpacityProperty, fuera);
        Escala.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 0.95, TimeSpan.FromMilliseconds(300)) { EasingFunction = suave });
        Escala.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 0.95, TimeSpan.FromMilliseconds(300)) { EasingFunction = suave });
    }

    private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static Color Aclarar(Color c, double t) => Mezclar(c, Colors.White, t);

    private static Color Mezclar(Color a, Color b, double t) => Color.FromRgb(
        (byte)(a.R + ((b.R - a.R) * t)),
        (byte)(a.G + ((b.G - a.G) * t)),
        (byte)(a.B + ((b.B - a.B) * t)));

    /// <summary>Muestra el aviso del modo indicado (cierra el anterior si seguía visible).</summary>
    public static void Mostrar(string modo, string? detalle = null)
    {
        if (_actual is not null)
        {
            try { _actual.Close(); } catch { /* ya cerrada */ }
        }
        var overlay = new VentanaOverlayModo(modo, detalle);
        overlay.Closed += (_, _) =>
        {
            if (ReferenceEquals(_actual, overlay))
            {
                _actual = null;
            }
        };
        _actual = overlay;
        overlay.Show();
    }
}
