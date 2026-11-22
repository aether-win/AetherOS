using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;

namespace CentroAether.Pages;

public sealed partial class LimpiezaPage : PaginaBase
{
    public LimpiezaPage()
    {
        this.InitializeComponent();
    }

    private async void Limpiar_Click(object sender, RoutedEventArgs e)
    {
        BtnLimpiar.IsEnabled = false;
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Limpiando...";
        Estado.IsOpen = true;

        string mb = (await Aether.Limpiar()).Trim();

        BtnLimpiar.IsEnabled = true;
        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = string.IsNullOrEmpty(mb) ? "Limpieza terminada." : $"Listo. Se liberaron unos {mb} MB.";
        Estado.IsOpen = true;
    }

    private async void Almacen_Click(object sender, RoutedEventArgs e)
    {
        await Aether.AbrirAlmacenamiento();
    }
}
