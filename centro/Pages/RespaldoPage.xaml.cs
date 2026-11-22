using System;
using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;

namespace CentroAether.Pages;

public sealed partial class RespaldoPage : PaginaBase
{
    private string? _destino;

    public RespaldoPage()
    {
        this.InitializeComponent();
    }

    private async void Carpeta_Click(object sender, RoutedEventArgs e)
    {
        // Selector de carpetas nativo de .NET 8 (WPF); reemplaza al FolderPicker de WinRT.
        var dialogo = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Elige dónde guardar el respaldo",
            Multiselect = false,
        };

        if (dialogo.ShowDialog(Window.GetWindow(this)) == true && !string.IsNullOrEmpty(dialogo.FolderName))
        {
            _destino = dialogo.FolderName;
            FilaDestino.Descripcion = _destino;
            BtnRespaldar.IsEnabled = true;
            await Aether.ConfigurarRespaldo(_destino);
        }
    }

    private async void Respaldar_Click(object sender, RoutedEventArgs e)
    {
        BtnRespaldar.IsEnabled = false;
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Haciendo el respaldo... puede tardar segun cuantos archivos tengas.";
        Estado.IsOpen = true;

        string salida = await Aether.HacerRespaldo();

        BtnRespaldar.IsEnabled = true;
        if (salida.Contains("ERROR", StringComparison.OrdinalIgnoreCase)
            || salida.Contains("No se", StringComparison.OrdinalIgnoreCase))
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = "No se pudo completar el respaldo. Revisa que el disco este conectado.";
        }
        else
        {
            Estado.Severity = InfoBarSeverity.Success;
            Estado.Message = "Respaldo terminado.";
        }
        Estado.IsOpen = true;
    }
}
