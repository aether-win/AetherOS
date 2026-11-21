using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;

namespace CentroAether;

/// <summary>
/// Overlay nativo de 470×260 que aparece al activar el Modo Gaming: sin bordes,
/// siempre al frente, fuera del Alt-Tab, aparece con un fundido y se cierra solo.
/// </summary>
public sealed partial class VentanaOverlayGaming : Window
{
    private const int Ancho = 470;
    private const int Alto = 260;

    private readonly DispatcherTimer _autoCierre = new() { Interval = TimeSpan.FromSeconds(3) };
    private Storyboard _fadeIn = null!;
    private Storyboard _fadeOut = null!;

    public VentanaOverlayGaming(string? subtitulo = null)
    {
        this.InitializeComponent();

        if (!string.IsNullOrWhiteSpace(subtitulo))
        {
            TxtSub.Text = subtitulo;
        }

        _fadeIn = (Storyboard)Raiz.Resources["FadeIn"];
        _fadeOut = (Storyboard)Raiz.Resources["FadeOut"];
        _fadeOut.Completed += FadeOut_Completed;

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

    private void FadeOut_Completed(object? sender, object e)
    {
        this.Close();
    }

    /// <summary>Crea, posiciona y muestra el overlay de Gaming.</summary>
    public static void Mostrar(string? subtitulo = null)
    {
        var overlay = new VentanaOverlayGaming(subtitulo);
        overlay.Activate();
    }
}
