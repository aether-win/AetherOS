using System;
using System.Threading.Tasks;
using System.Windows;
using CentroAether.Controles;
using CentroAether.Services;

namespace CentroAether.Pages;

/// <summary>
/// Inicio: panel con el modo y la batería, cambio de modo en un clic, accesos rápidos a
/// las secciones más usadas y el tema de Windows.
/// </summary>
public sealed partial class InicioPage : PaginaBase
{
    private bool _ocupado;

    public InicioPage()
    {
        InitializeComponent();
        ConstruirModos();
        ConstruirAccesos();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await RefrescarResumen();
    }

    private async Task RefrescarResumen()
    {
        if (!Aether.Disponible)
        {
            TxtModoHero.Text = "—";
            TxtBateriaHero.Text = "—";
            Aviso("No se encontró la lógica de Aether en este equipo.", InfoBarSeverity.Warning);
            return;
        }
        string modo = await Aether.ModoActual();
        TxtModoHero.Text = Ui.NombreModo(modo);
        string bateria = (await Aether.Bateria()).Trim();
        if (bateria == "SIN" || string.IsNullOrEmpty(bateria))
        {
            ChipBateria.Visibility = Visibility.Collapsed;
        }
        else
        {
            TxtBateriaHero.Text = $"{bateria}%";
        }
    }

    private void ConstruirModos()
    {
        GridModos.Children.Add(Ui.Mosaico("", "Trabajo", "Todo activo, animaciones completas y equilibrado.",
            Ui.ColorModo("Trabajo"), () => _ = CambiarModo("Trabajo")));
        GridModos.Children.Add(Ui.Mosaico("", "Gaming", "Máximo rendimiento. Necesita el cargador conectado.",
            Ui.ColorModo("Gaming"), () => _ = CambiarModo("Gaming")));
        GridModos.Children.Add(Ui.Mosaico("", "Batería", "Eficiente para durar más lejos del enchufe.",
            Ui.ColorModo("Bateria"), () => _ = CambiarModo("Bateria")));
    }

    private void ConstruirAccesos()
    {
        (string glyph, string titulo, string desc, string color, string id)[] accesos =
        {
            ("", "Reparar la red", "Carpetas compartidas e internet", "#22D3EE", "red"),
            ("", "Liberar espacio", "Temporales y papelera", "#3FBF7F", "boost"),
            ("", "Actualizaciones", "Windows y tus programas", "#2F8CFF", "actualizar"),
            ("", "Respaldo", "Copia tus archivos", "#F2B33D", "respaldo"),
            ("", "Monitor", "CPU y RAM en vivo", "#7A5AF8", "monitor"),
            ("", "Mandos", "PS4 como mando de Xbox", "#2F8CFF", "control"),
            ("", "Wind-Aether", "Mods visuales", "#E879F9", "wind"),
            ("", "Software", "Apps confiables en un clic", "#4FC3F7", "software"),
        };
        foreach (var a in accesos)
        {
            string id = a.id;
            GridAccesos.Children.Add(Ui.Mosaico(a.glyph, a.titulo, a.desc, Ui.Hex(a.color), () => IrA(id)));
        }
    }

    private async Task CambiarModo(string modo)
    {
        if (_ocupado)
        {
            return;
        }
        if (!Aether.Disponible)
        {
            Aviso("Aether OS no está instalado en este equipo, así que no se puede cambiar de modo.", InfoBarSeverity.Warning);
            return;
        }
        _ocupado = true;
        GridModos.IsEnabled = false;
        Aviso($"Cargando modo {Ui.NombreModo(modo)}…", InfoBarSeverity.Informational);

        (bool ok, string mensaje) = await Aether.CambiarModoDetallado(modo);

        GridModos.IsEnabled = true;
        _ocupado = false;
        if (!ok)
        {
            Aviso(mensaje, InfoBarSeverity.Warning);
        }
        else
        {
            Aviso($"Modo {Ui.NombreModo(modo)} activo.", InfoBarSeverity.Success);
            VentanaOverlayModo.Mostrar(modo);
        }
        await RefrescarResumen();
        SegundoPlano.ModoCambiado(Aether.ModoGuardado());
    }

    private async void Oscuro_Click(object sender, RoutedEventArgs e)
    {
        await Aether.AplicarTema("oscuro");
        Aviso("Tema oscuro aplicado.", InfoBarSeverity.Success);
    }

    private async void Claro_Click(object sender, RoutedEventArgs e)
    {
        await Aether.AplicarTema("claro");
        Aviso("Tema claro aplicado.", InfoBarSeverity.Success);
    }

    private void Aviso(string texto, InfoBarSeverity tipo)
    {
        Estado.Severity = tipo;
        Estado.Message = texto;
        Estado.IsOpen = true;
    }
}
