using CentroAether.Models;
using System.Windows.Controls;
using CentroAether.Controles;

namespace CentroAether.Pages;

/// <summary>
/// Página genérica para módulos registrados pero todavía sin implementar.
/// Muestra el nombre, la descripción y la lista de funciones planeadas que
/// vienen del <see cref="Modulo"/> recibido como parámetro de navegación.
/// </summary>
public sealed partial class ProximamentePage : PaginaBase
{
    public ProximamentePage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is Modulo m)
        {
            Cabecera.Titulo = m.Nombre;
            Cabecera.Subtitulo = m.Descripcion;
            Cabecera.Glyph = m.Glyph;
            ListaCaracteristicas.ItemsSource = m.Caracteristicas;
        }
    }
}
