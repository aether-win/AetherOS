using System;
using System.Collections.Generic;
using CentroAether.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

/// <summary>
/// Aether Controller (base): detecta mandos conectados vía Windows.Gaming.Input.
/// El motor de mapeo (emulación Xbox 360, perfiles) basado en DS4Windows llegará después.
/// </summary>
public sealed partial class ControllerPage : Page
{
    private bool _ocupado;
    private bool _cargando;

    public ControllerPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Buscar();
        RefrescarMotor();
    }

    private void RefrescarMotor()
    {
        bool vigem = MotorControlador.ViGEmDisponible();

        _cargando = true;
        TglEmular.IsOn = MotorControlador.Activo;
        _cargando = false;

        TglEmular.IsEnabled = vigem;
        PanelRequisitos.Visibility = vigem ? Visibility.Collapsed : Visibility.Visible;

        if (!vigem)
        {
            TxtMotor.Text = "Falta el driver ViGEmBus. Instálalo para activar la emulación.";
        }
        else
        {
            TxtMotor.Text = MotorControlador.Activo
                ? "Activa: tu mando se refleja como un Xbox 360 virtual."
                : "Lista para activar. Convierte tu mando en un Xbox 360 virtual.";
        }
    }

    private void Emular_Toggled(object sender, RoutedEventArgs e)
    {
        if (_cargando)
        {
            return;
        }

        if (TglEmular.IsOn)
        {
            if (!MotorControlador.ViGEmDisponible())
            {
                _cargando = true;
                TglEmular.IsOn = false;
                _cargando = false;
                RefrescarMotor();
                return;
            }
            bool ok = MotorControlador.Iniciar();
            Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
            Estado.Message = ok ? "Emulación Xbox 360 activada." : "No se pudo iniciar la emulación.";
            Estado.IsOpen = true;
        }
        else
        {
            MotorControlador.Detener();
            Estado.Severity = InfoBarSeverity.Informational;
            Estado.Message = "Emulación detenida.";
            Estado.IsOpen = true;
        }
        RefrescarMotor();
    }

    private async void Vigem_Click(object sender, RoutedEventArgs e)
        => await InstalarRequisito(BtnVigem, "Nefarius.ViGEmBus", "ViGEmBus");

    private async void HidHide_Click(object sender, RoutedEventArgs e)
        => await InstalarRequisito(BtnHidHide, "Nefarius.HidHide", "HidHide");

    private async System.Threading.Tasks.Task InstalarRequisito(Button btn, string wingetId, string nombre)
    {
        if (_ocupado)
        {
            return;
        }
        _ocupado = true;
        btn.IsEnabled = false;
        string original = btn.Content?.ToString() ?? "Instalar";
        btn.Content = "Instalando…";
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = $"Instalando {nombre}…";
        Estado.IsOpen = true;

        string salida = await Aether.InstalarWinget(wingetId);

        bool ok = !salida.Contains("ERROR", StringComparison.OrdinalIgnoreCase);
        btn.IsEnabled = true;
        btn.Content = original;
        Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        Estado.Message = ok
            ? $"{nombre} instalado. Puede que necesites reiniciar para que el driver cargue."
            : $"No se pudo instalar {nombre}.";
        Estado.IsOpen = true;
        _ocupado = false;
        RefrescarMotor();
    }

    private void Refrescar_Click(object sender, RoutedEventArgs e) => Buscar();

    private void Buscar()
    {
        Contenedor.Children.Clear();
        List<MandoInfo> mandos = MandosServicio.Detectar();
        Vacio.Visibility = mandos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (MandoInfo m in mandos)
        {
            Contenedor.Children.Add(CrearTarjeta(m));
        }
    }

    private Border CrearTarjeta(MandoInfo m)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        sp.Children.Add(new FontIcon
        {
            Glyph = "",
            FontSize = 20,
            VerticalAlignment = VerticalAlignment.Center,
        });

        var textos = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        textos.Children.Add(new TextBlock { Text = m.Nombre, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        textos.Children.Add(new TextBlock
        {
            Text = m.EsGamepad ? "Conectado · compatible" : "Conectado",
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"],
        });
        sp.Children.Add(textos);

        return new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Child = sp,
        };
    }

    private async void Ds4_Click(object sender, RoutedEventArgs e)
    {
        if (_ocupado)
        {
            return;
        }
        _ocupado = true;
        BtnDs4.IsEnabled = false;
        BtnDs4.Content = "Instalando…";
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Instalando DS4Windows…";
        Estado.IsOpen = true;

        string salida = await Aether.InstalarWinget("Ryochan7.DS4Windows");

        bool ok = !salida.Contains("ERROR", StringComparison.OrdinalIgnoreCase);
        BtnDs4.IsEnabled = true;
        BtnDs4.Content = ok ? "Listo" : "Reintentar";
        Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        Estado.Message = ok ? "DS4Windows instalado o ya estaba al día." : "No se pudo instalar DS4Windows.";
        Estado.IsOpen = true;
        _ocupado = false;
    }
}
