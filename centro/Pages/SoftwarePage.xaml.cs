using System;
using System.Linq;
using System.Threading.Tasks;
using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using CentroAether.Controles;
using System.Windows.Media;

namespace CentroAether.Pages;

/// <summary>
/// Software Center: catálogo curado de apps confiables instalables con winget.
/// </summary>
public sealed partial class SoftwarePage : PaginaBase
{
    private bool _ocupado;

    public SoftwarePage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Construir();
    }

    private void Construir()
    {
        Contenedor.Children.Clear();
        foreach (string categoria in CatalogoApps.Todas.Select(a => a.Categoria).Distinct())
        {
            Contenedor.Children.Add(Ui.Seccion(categoria));
            var rejilla = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, -10, -10) };
            foreach (AppCatalogo app in CatalogoApps.Todas.Where(a => a.Categoria == categoria))
            {
                rejilla.Children.Add(CrearTarjeta(app));
            }
            Contenedor.Children.Add(rejilla);
        }
    }

    private FilaAjuste CrearTarjeta(AppCatalogo app)
    {
        var btn = new Button { Content = "Instalar" };
        btn.Click += async (_, _) => await Instalar(app, btn);

        return new FilaAjuste
        {
            Glyph = app.Glyph,
            Titulo = app.Nombre,
            Descripcion = app.Descripcion,
            Accion = btn,
            Margin = new Thickness(0, 0, 10, 10),
        };
    }

    private async Task Instalar(AppCatalogo app, Button btn)
    {
        if (_ocupado)
        {
            return;
        }
        _ocupado = true;
        btn.IsEnabled = false;
        btn.Content = "Instalando…";
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = $"Instalando {app.Nombre}…";
        Estado.IsOpen = true;

        string salida = await Aether.InstalarWinget(app.WingetId);

        bool ok = !salida.Contains("ERROR", StringComparison.OrdinalIgnoreCase);
        btn.IsEnabled = true;
        btn.Content = ok ? "Listo" : "Reintentar";
        Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        Estado.Message = ok
            ? $"{app.Nombre} quedó instalado o ya estaba al día."
            : $"No se pudo instalar {app.Nombre}.";
        Estado.IsOpen = true;
        _ocupado = false;
    }

    private async void ActualizarTodo_Click(object sender, RoutedEventArgs e)
    {
        if (_ocupado)
        {
            return;
        }
        _ocupado = true;
        BtnActualizarTodo.IsEnabled = false;
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Buscando actualizaciones de todo lo instalado…";
        Estado.IsOpen = true;

        await Aether.ActualizarProgramas();

        BtnActualizarTodo.IsEnabled = true;
        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = "Listo. Se actualizó lo que tenía versión nueva.";
        Estado.IsOpen = true;
        _ocupado = false;
    }
}
