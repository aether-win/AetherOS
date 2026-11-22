using System.Collections.Generic;
using System.Linq;
using CentroAether.Services;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;
using System.Windows.Media;

namespace CentroAether.Pages;

/// <summary>
/// Aether Profiles: CRUD de perfiles que asocian una app a un modo. Se guardan en
/// ProgramData\Aether\perfiles.json. La aplicación automática llega con Background Intelligence.
/// </summary>
public sealed partial class PerfilesPage : PaginaBase
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

    private FilaAjuste CrearTarjeta(PerfilApp p)
    {
        string detalle = $"Modo {Ui.NombreModo(p.Modo)}";
        if (!string.IsNullOrEmpty(p.Proceso))
        {
            detalle += $" · {p.Proceso}";
        }
        if (!string.IsNullOrEmpty(p.MandoVirtual))
        {
            detalle += p.MandoVirtual == "DualShock4" ? " · Mando: DualShock 4" : " · Mando: Xbox 360";
        }

        var btn = new Button { Content = "Quitar" };
        btn.Click += (_, _) =>
        {
            _perfiles.Remove(p);
            PerfilesServicio.Guardar(_perfiles);
            Pintar();
        };

        return new FilaAjuste
        {
            Glyph = Ui.GlyphModo(p.Modo),
            ColorIcono = new SolidColorBrush(Ui.ColorModo(p.Modo)),
            Titulo = p.App,
            Descripcion = detalle,
            Accion = btn,
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
