using System.Collections.Generic;
using System.Linq;
using CentroAether.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

/// <summary>
/// Aether Profiles: CRUD de perfiles que asocian una app a un modo. Se guardan en
/// ProgramData\Aether\perfiles.json. La aplicación automática llega con Background Intelligence.
/// </summary>
public sealed partial class PerfilesPage : Page
{
    private List<PerfilApp> _perfiles = new();
    private bool _cargando;

    public PerfilesPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _cargando = true;
        TglInteligencia.IsOn = ConfigCentro.InteligenciaActiva;
        _cargando = false;
        _perfiles = PerfilesServicio.Cargar();
        Pintar();
    }

    private void Inteligencia_Toggled(object sender, RoutedEventArgs e)
    {
        if (_cargando)
        {
            return;
        }
        ConfigCentro.InteligenciaActiva = TglInteligencia.IsOn;
        InteligenciaFondo.Refrescar();
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = TglInteligencia.IsOn
            ? "Segundo plano activado: los perfiles se aplicarán al abrir cada app."
            : "Segundo plano desactivado.";
        Estado.IsOpen = true;
    }

    private void Pintar()
    {
        Contenedor.Children.Clear();
        TxtVacio.Visibility = _perfiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (PerfilApp p in _perfiles)
        {
            Contenedor.Children.Add(CrearTarjeta(p));
        }
    }

    private Border CrearTarjeta(PerfilApp p)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textos = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        textos.Children.Add(new TextBlock { Text = p.App, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        string detalle = $"Modo {p.Modo}";
        if (!string.IsNullOrEmpty(p.Proceso))
        {
            detalle += $" · {p.Proceso}";
        }
        if (!string.IsNullOrEmpty(p.MandoVirtual))
        {
            detalle += p.MandoVirtual == "DualShock4" ? " · Mando: DualShock 4" : " · Mando: Xbox 360";
        }
        textos.Children.Add(new TextBlock
        {
            Text = detalle,
            FontSize = 13,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        Grid.SetColumn(textos, 0);
        grid.Children.Add(textos);

        var btn = new Button { Content = "Quitar", VerticalAlignment = VerticalAlignment.Center };
        btn.Click += (_, _) =>
        {
            _perfiles.Remove(p);
            PerfilesServicio.Guardar(_perfiles);
            Pintar();
        };
        Grid.SetColumn(btn, 1);
        grid.Children.Add(btn);

        return new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Child = grid,
        };
    }

    private void Agregar_Click(object sender, RoutedEventArgs e)
    {
        string app = TxtApp.Text.Trim();
        if (string.IsNullOrEmpty(app))
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = "Escribe el nombre de la app o juego.";
            Estado.IsOpen = true;
            return;
        }

        string modo = (CmbModo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Gaming";

        if (_perfiles.Any(x => x.App.Equals(app, System.StringComparison.OrdinalIgnoreCase)))
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = $"Ya existe un perfil para «{app}».";
            Estado.IsOpen = true;
            return;
        }

        string mando = CmbMando.SelectedIndex switch
        {
            1 => "Xbox360",
            2 => "DualShock4",
            _ => "",
        };

        _perfiles.Add(new PerfilApp { App = app, Proceso = TxtProceso.Text.Trim(), Modo = modo, MandoVirtual = mando });
        bool ok = PerfilesServicio.Guardar(_perfiles);
        TxtApp.Text = "";
        TxtProceso.Text = "";
        CmbMando.SelectedIndex = 0;
        Pintar();

        Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        Estado.Message = ok ? $"Perfil para «{app}» guardado." : "No se pudo guardar el perfil.";
        Estado.IsOpen = true;
    }
}
