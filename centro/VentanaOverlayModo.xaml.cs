using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using Windows.UI;

namespace CentroAether;

/// <summary>
/// Overlay nativo de 470×260 que aparece al activar un modo (Gaming, Trabajo o
/// Batería). Cada modo tiene su color, icono y texto. Sin bordes, siempre al
/// frente, fuera del Alt-Tab, con fundido y auto-cierre.
/// </summary>
public sealed partial class VentanaOverlayModo : Window
{
    private const int Ancho = 470;
    private const int Alto = 260;

    private readonly DispatcherTimer _autoCierre = new() { Interval = TimeSpan.FromSeconds(3) };
    private Storyboard _fadeIn = null!;
    private Storyboard _fadeOut = null!;

    public VentanaOverlayModo(string modo, string? detalle = null)
    {
        this.InitializeComponent();
        Aplicar(modo, detalle);

        _fadeIn = (Storyboard)Raiz.Resources["FadeIn"];
        _fadeOut = (Storyboard)Raiz.Resources["FadeOut"];
        _fadeOut.Completed += (_, _) => this.Close();

        ConfigurarVentana();

        _autoCierre.Tick += (_, _) =>
        {
            _autoCierre.Stop();
            _fadeOut.Begin();
        };

        Raiz.Loaded += (_, _) =>
        {
            _fadeIn.Begin();
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
        Barra.Foreground = brocha;
        Franja.Background = brocha;
    }

    private void ConfigurarVentana()
    {
        if (AppWindow.Presenter is OverlappedPresenter pres)
        {
            pres.SetBorderAndTitleBar(false, false);
            pres.IsAlwaysOnTop = true;
            pres.IsResizable = false;
            pres.IsMaximizable = false;
            pres.IsMinimizable = false;
        }

        AppWindow.IsShownInSwitchers = false;
        AppWindow.Resize(new SizeInt32(Ancho, Alto));

        DisplayArea area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        if (area is not null)
        {
            int x = area.WorkArea.X + ((area.WorkArea.Width - Ancho) / 2);
            int y = area.WorkArea.Y + 72;
            AppWindow.Move(new PointInt32(x, y));
        }
    }

    private static Color Hex(string hex)
    {
        hex = hex.TrimStart('#');
        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
        return Color.FromArgb(255, r, g, b);
    }

    /// <summary>Crea, posiciona y muestra el overlay del modo indicado.</summary>
    public static void Mostrar(string modo, string? detalle = null)
    {
        var overlay = new VentanaOverlayModo(modo, detalle);
        overlay.Activate();
    }
}
