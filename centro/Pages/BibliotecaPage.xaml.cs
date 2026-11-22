using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;
using System.Windows.Media;

namespace CentroAether.Pages;

/// <summary>
/// Game Library (base): detecta juegos de Steam y permite lanzarlos. Opcionalmente
/// activa el Modo Gaming y muestra el overlay al iniciar un juego.
/// </summary>
public sealed partial class BibliotecaPage : PaginaBase
{
    public BibliotecaPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Buscar();
    }

    private void Refrescar_Click(object sender, RoutedEventArgs e) => Buscar();

    private void Buscar()
    {
        Contenedor.Children.Clear();
        List<JuegoInfo> juegos = BibliotecaServicio.DetectarSteam();

        Vacio.Visibility = juegos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (JuegoInfo j in juegos)
        {
            Contenedor.Children.Add(CrearTarjeta(j));
        }
    }

    private FilaAjuste CrearTarjeta(JuegoInfo j)
    {
        var btn = new Button
        {
            Style = (Style)Application.Current.Resources["AccentButtonStyle"],
            Content = "Jugar",
        };
        btn.Click += async (_, _) => await Jugar(j);

        return new FilaAjuste
        {
            Glyph = "\uE7FC",
            Titulo = j.Nombre,
            Descripcion = j.Tienda,
            Accion = btn,
            Margin = new Thickness(0, 0, 10, 10),
        };
    }

    private async Task Jugar(JuegoInfo j)
    {
        if (ModoAlJugar.IsOn)
        {
            (bool ok, string mensaje) = await Aether.CambiarModoDetallado("Gaming");
            if (ok)
            {
                VentanaOverlayModo.Mostrar("Gaming", j.Nombre);
            }
            else
            {
                SegundoPlano.Notificar(mensaje);
            }
            SegundoPlano.ModoCambiado(Aether.ModoGuardado());
        }

        try
        {
            Sistema.Abrir($"steam://rungameid/{j.AppId}");
            Estado.Severity = InfoBarSeverity.Success;
            Estado.Message = $"Iniciando {j.Nombre}…";
        }
        catch (Exception ex)
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = $"No se pudo iniciar {j.Nombre}: {ex.Message}";
        }
        Estado.IsOpen = true;
    }
}
