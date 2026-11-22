using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;
using System.Windows.Media;

namespace CentroAether.Pages;

/// <summary>Compatibility Database: estado de apps/funciones tras los ajustes de Aether.</summary>
public sealed partial class CompatibilidadPage : PaginaBase
{
    public CompatibilidadPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Contenedor.Children.Clear();
        foreach (EntradaCompat c in CompatibilidadServicio.Todas)
        {
            Contenedor.Children.Add(CrearTarjeta(c));
        }
    }

    private FilaAjuste CrearTarjeta(EntradaCompat c)
    {
        return new FilaAjuste
        {
            Glyph = c.Ok ? "\uE73E" : "\uE7BA",
            ColorIcono = Ui.Recurso(c.Ok ? "SystemFillColorSuccessBrush" : "SystemFillColorCautionBrush"),
            Titulo = c.Nombre,
            Descripcion = c.Nota,
            Accion = Ui.Insignia(c.Ok ? "Funciona" : "Con aviso", Ui.Hex(c.Ok ? "#3FBF7F" : "#F2B33D")),
        };
    }
}
