using System.Threading.Tasks;
using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;
using System.Windows.Media;

namespace CentroAether.Pages;

/// <summary>
/// Runtime Manager: detecta VC++, .NET, DirectX, WebView2 y Python, y permite
/// instalar los que falten usando los paquetes oficiales de Microsoft (winget).
/// </summary>
public sealed partial class RuntimesPage : PaginaBase
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

    private FilaAjuste CrearTarjeta(RuntimeInfo rt)
    {
        object accion;
        if (rt.Instalado)
        {
            accion = Ui.Insignia(rt.Version == "instalado" ? "Instalado" : $"v{rt.Version}", Ui.Hex("#3FBF7F"));
        }
        else
        {
            var btn = new Button { Content = "Instalar", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
            btn.Click += async (_, _) => await Instalar(rt, btn);
            accion = btn;
        }

        return new FilaAjuste
        {
            Glyph = rt.Instalado ? "\uE73E" : "\uE896",
            ColorIcono = rt.Instalado ? Ui.Recurso("SystemFillColorSuccessBrush") : null,
            Titulo = rt.Nombre,
            Descripcion = rt.Descripcion,
            Accion = accion,
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
