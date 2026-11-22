using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CentroAether;

/// <summary>
/// Overlay de 470×260 que aparece al activar un modo (Gaming, Trabajo o Batería).
/// Cada modo tiene su color, icono y texto. Sin bordes, siempre al frente, fuera de
/// la barra de tareas, sin robar el foco, con fundido y auto-cierre a los 3 segundos.
/// </summary>
public partial class VentanaOverlayModo : Window
{
    private readonly DispatcherTimer _autoCierre = new() { Interval = TimeSpan.FromSeconds(3) };

    public VentanaOverlayModo(string modo, string? detalle = null)
    {
        InitializeComponent();
        Aplicar(modo, detalle);
        Posicionar();

        _autoCierre.Tick += (_, _) =>
        {
            _autoCierre.Stop();
            var fuera = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
            fuera.Completed += (_, _) => Close();
            Raiz.BeginAnimation(OpacityProperty, fuera);
        };

        Loaded += (_, _) =>
        {
            Raiz.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280)));
            AnimarBarra();
            _autoCierre.Start();
        };
    }

    private void Aplicar(string modo, string? detalle)
    {
        (string titulo, string sub, string glyph, Color color) = modo.ToLowerInvariant() switch
        {
            "gaming" => ("MODO GAMING", "Máximo rendimiento", "", Hex("#3A7BD5")),
            "trabajo" => ("MODO TRABAJO", "Equilibrado para el día a día", "", Hex("#7A5AF8")),
            "bateria" => ("MODO BATERÍA", "Optimizado para durar más", "", Hex("#2FA86A")),
            _ => ($"MODO {modo.ToUpperInvariant()}", "", "", Hex("#3A7BD5")),
        };

        TxtTitulo.Text = titulo;
        TxtSub.Text = string.IsNullOrWhiteSpace(detalle) ? sub : detalle!;
        Icono.Glyph = glyph;

        var brocha = new SolidColorBrush(color);
        Marco.BorderBrush = brocha;
        CajaIcono.Background = brocha;
        Barra.Background = brocha;
        Franja.Background = brocha;
    }

    private void Posicionar()
    {
        Rect area = SystemParameters.WorkArea;
        Left = area.Left + ((area.Width - Width) / 2);
        Top = area.Top + 72;
    }

    private void AnimarBarra()
    {
        double ancho = Carril.ActualWidth > 0 ? Carril.ActualWidth : 300;
        var deslizar = new DoubleAnimation(-Barra.Width, ancho, TimeSpan.FromMilliseconds(1100))
        {
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        Barra.BeginAnimation(Canvas.LeftProperty, deslizar);
    }

    private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    /// <summary>Crea, posiciona y muestra el overlay del modo indicado.</summary>
    public static void Mostrar(string modo, string? detalle = null)
    {
        var overlay = new VentanaOverlayModo(modo, detalle);
        overlay.Show();
    }
}
