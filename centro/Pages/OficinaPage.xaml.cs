using System;
using CentroAether.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

/// <summary>
/// Office Protection: detecta Office, protege la seguridad de macros y da acceso
/// al modo enfocado. No desactiva funciones de Office.
/// </summary>
public sealed partial class OficinaPage : Page
{
    public OficinaPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Refrescar();
    }

    private void Refrescar()
    {
        bool office = OficinaServicio.OfficeInstalado();
        TxtOffice.Text = office
            ? "Microsoft Office detectado en este equipo."
            : "No se detectó Microsoft Office. Las protecciones se aplicarán si lo instalas.";

        bool protegidas = OficinaServicio.MacrosProtegidas();
        TxtMacros.Text = protegidas
            ? "Protegidas: las macros se bloquean con aviso antes de ejecutarse."
            : "Las macros podrían ejecutarse sin aviso. Recomendado protegerlas.";
        BtnMacros.IsEnabled = !protegidas;
        BtnMacros.Content = protegidas ? "Protegidas" : "Proteger macros";
    }

    private void Macros_Click(object sender, RoutedEventArgs e)
    {
        bool ok = OficinaServicio.ProtegerMacros();
        Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        Estado.Message = ok
            ? "Macros protegidas. Reinicia las apps de Office que estén abiertas."
            : "No se pudo aplicar la protección.";
        Estado.IsOpen = true;
        Refrescar();
    }

    private async void Enfocado_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:quiethours"));
        }
        catch
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = "No se pudo abrir la configuración de concentración.";
            Estado.IsOpen = true;
        }
    }
}
