using System;
using CentroAether.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace CentroAether.Pages;

public sealed partial class RespaldoPage : Page
{
    private string? _destino;

    public RespaldoPage()
    {
        this.InitializeComponent();
    }

    private async void Carpeta_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        if (App.MainWindow is not null)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }

        var carpeta = await picker.PickSingleFolderAsync();
        if (carpeta is not null)
        {
            _destino = carpeta.Path;
            TxtDestino.Text = _destino;
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
