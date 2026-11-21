using CentroAether.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

/// <summary>
/// Página genérica para módulos registrados pero todavía sin implementar.
/// Muestra el nombre, la descripción y la lista de funciones planeadas que
/// vienen del <see cref="Modulo"/> recibido como parámetro de navegación.
/// </summary>
public sealed partial class ProximamentePage : Page
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
            TxtNombre.Text = m.Nombre;
            TxtDescripcion.Text = m.Descripcion;
            IconoModulo.Glyph = m.Glyph;
            ListaCaracteristicas.ItemsSource = m.Caracteristicas;
        }
    }
}
