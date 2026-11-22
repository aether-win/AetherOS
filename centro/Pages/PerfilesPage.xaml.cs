using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CentroAether.Controles;
using CentroAether.Services;

namespace CentroAether.Pages;

/// <summary>
/// Perfiles por juego/app: modo y mando, selector de procesos, editar, pausar, borrar, mando
/// predeterminado al cerrar el juego y sonidos de cambio de mando.
/// </summary>
public sealed partial class PerfilesPage : PaginaBase
{
    private List<PerfilApp> _perfiles = new();
    private bool _cargando;
    private int _editando = -1;
    private string _rutaElegida = "";

    private static readonly string[] Modos = { "", "Gaming", "Trabajo", "Bateria" };
    private static readonly string[] Mandos = { "", "Xbox360", "DualShock4" };

    public PerfilesPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _cargando = true;
        TglInteligencia.IsOn = ConfigCentro.InteligenciaActiva;
        TglSonidos.IsOn = ConfigCentro.SonidosMandos;
        CmbPredeterminado.SelectedIndex = Math.Max(0, Array.IndexOf(Mandos, ConfigCentro.MandoPredeterminado));
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
        Aviso(TglInteligencia.IsOn
            ? "Perfiles automáticos activados: se aplican al abrir cada juego y se deshacen al cerrarlo."
            : "Perfiles automáticos desactivados.", InfoBarSeverity.Informational);
        Pintar();
    }

    private void Predeterminado_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando || CmbPredeterminado.SelectedIndex < 0)
        {
            return;
        }
        ConfigCentro.MandoPredeterminado = Mandos[CmbPredeterminado.SelectedIndex];
        Aviso("Mando predeterminado guardado.", InfoBarSeverity.Success);
    }

    private void Sonidos_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_cargando)
        {
            ConfigCentro.SonidosMandos = TglSonidos.IsOn;
        }
    }

    private void ElegirProceso_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new VentanaProcesos { Owner = Window.GetWindow(this) };
        if (ventana.ShowDialog() == true && ventana.Elegido is ProcesoInfo p)
        {
            TxtApp.Text = p.Nombre;
            TxtProceso.Text = p.Ejecutable;
            _rutaElegida = p.Ruta;
            Aviso($"Elegido: {p.Nombre} ({p.Ejecutable}). Revisa el modo y el mando y pulsa {(_editando >= 0 ? "Guardar" : "Agregar")}.",
                InfoBarSeverity.Informational);
        }
    }

    private void Pintar()
    {
        Contenedor.Children.Clear();
        TxtVacio.Visibility = _perfiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        FilaAuto.Nota = InteligenciaFondo.PerfilAplicado is string aplicado ? $"Aplicado ahora: {aplicado}" : "";
        FilaAuto.ColorNota = Ui.Recurso("SystemFillColorSuccessBrush");
        for (int i = 0; i < _perfiles.Count; i++)
        {
            Contenedor.Children.Add(CrearTarjeta(_perfiles[i], i));
        }
    }

    private FilaAjuste CrearTarjeta(PerfilApp p, int indice)
    {
        var partes = new List<string>();
        if (!string.IsNullOrEmpty(p.Proceso))
        {
            partes.Add(p.Proceso);
        }
        partes.Add(string.IsNullOrEmpty(p.Modo) ? "Modo: no cambia" : $"Modo {Ui.NombreModo(p.Modo)}");
        partes.Add(p.MandoVirtual switch
        {
            "DualShock4" => "Mando: PS4",
            "Xbox360" => "Mando: Xbox 360",
            _ => "Mando: no cambia",
        });

        var activo = new ToggleSwitch { IsOn = p.Activo, VerticalAlignment = VerticalAlignment.Center, ToolTip = "Activar o pausar este perfil" };
        activo.Toggled += (_, _) =>
        {
            p.Activo = activo.IsOn;
            PerfilesServicio.Guardar(_perfiles);
            Aviso(p.Activo ? $"Perfil «{p.App}» activado." : $"Perfil «{p.App}» en pausa.", InfoBarSeverity.Informational);
        };
        var editar = new Button { Content = "Editar" };
        editar.Click += (_, _) => Editar(indice);
        var quitar = new Button { Content = "Quitar" };
        quitar.Click += (_, _) =>
        {
            _perfiles.RemoveAt(indice);
            PerfilesServicio.Guardar(_perfiles);
            if (_editando == indice)
            {
                Limpiar();
            }
            Pintar();
            Aviso($"Perfil «{p.App}» quitado.", InfoBarSeverity.Informational);
        };
        var acciones = new Pila { Orientation = Orientation.Horizontal, Spacing = 8 };
        acciones.Children.Add(activo);
        acciones.Children.Add(editar);
        acciones.Children.Add(quitar);

        bool aplicado = string.Equals(InteligenciaFondo.PerfilAplicado, p.App, StringComparison.OrdinalIgnoreCase);
        return new FilaAjuste
        {
            Glyph = string.IsNullOrEmpty(p.Modo) ? "" : Ui.GlyphModo(p.Modo),
            ColorIcono = new SolidColorBrush(string.IsNullOrEmpty(p.Modo) ? Ui.Hex("#4FC3F7") : Ui.ColorModo(p.Modo)),
            Titulo = p.App,
            Descripcion = string.Join(" · ", partes),
            Nota = aplicado ? "Aplicado ahora" : p.Activo ? "" : "En pausa",
            ColorNota = Ui.Recurso(aplicado ? "SystemFillColorSuccessBrush" : "TextFillColorTertiaryBrush"),
            Accion = acciones,
            Opacity = p.Activo ? 1.0 : 0.7,
        };
    }

    private void Editar(int indice)
    {
        PerfilApp p = _perfiles[indice];
        _editando = indice;
        TxtApp.Text = p.App;
        TxtProceso.Text = p.Proceso;
        _rutaElegida = p.Ruta;
        CmbModo.SelectedIndex = Math.Max(0, Array.IndexOf(Modos, p.Modo ?? ""));
        CmbMando.SelectedIndex = Math.Max(0, Array.IndexOf(Mandos, p.MandoVirtual ?? ""));
        TxtTituloForm.Text = $"Editar perfil «{p.App}»";
        TxtAgregar.Text = "Guardar cambios";
        IcoAgregar.Glyph = "";
        BtnCancelar.Visibility = Visibility.Visible;
        TxtApp.Focus();
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => Limpiar();

    private void Limpiar()
    {
        _editando = -1;
        _rutaElegida = "";
        TxtApp.Text = "";
        TxtProceso.Text = "";
        CmbModo.SelectedIndex = 1;
        CmbMando.SelectedIndex = 0;
        TxtTituloForm.Text = "Nuevo perfil";
        TxtAgregar.Text = "Agregar perfil";
        IcoAgregar.Glyph = "";
        BtnCancelar.Visibility = Visibility.Collapsed;
    }

    private void Agregar_Click(object sender, RoutedEventArgs e)
    {
        string app = TxtApp.Text.Trim();
        string proceso = TxtProceso.Text.Trim();
        if (string.IsNullOrEmpty(app) || string.IsNullOrEmpty(proceso))
        {
            Aviso("Falta el nombre o el proceso. Lo más fácil: «Elegir de los programas abiertos…».", InfoBarSeverity.Warning);
            return;
        }
        if (!proceso.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            proceso += ".exe";
        }
        string modo = Modos[Math.Max(0, CmbModo.SelectedIndex)];
        string mando = Mandos[Math.Max(0, CmbMando.SelectedIndex)];
        if (modo.Length == 0 && mando.Length == 0)
        {
            Aviso("El perfil no cambia nada: elige un modo, un mando o ambos.", InfoBarSeverity.Warning);
            return;
        }

        // Conflictos: el mismo proceso no puede estar en dos perfiles.
        int otro = _perfiles.FindIndex(x => x.Proceso.Equals(proceso, StringComparison.OrdinalIgnoreCase));
        if (otro >= 0 && otro != _editando)
        {
            Aviso($"{proceso} ya tiene el perfil «{_perfiles[otro].App}». Edítalo en lugar de crear otro.", InfoBarSeverity.Warning);
            return;
        }

        var nuevo = new PerfilApp
        {
            App = app,
            Proceso = proceso,
            Ruta = _rutaElegida,
            Modo = modo,
            MandoVirtual = mando,
            Activo = _editando >= 0 ? _perfiles[_editando].Activo : true,
        };
        bool editando = _editando >= 0;
        if (editando)
        {
            _perfiles[_editando] = nuevo;
        }
        else
        {
            _perfiles.Add(nuevo);
        }
        bool ok = PerfilesServicio.Guardar(_perfiles);
        Limpiar();
        Pintar();
        Aviso(ok
            ? $"Perfil «{app}» {(editando ? "actualizado" : "guardado")}." + (ConfigCentro.InteligenciaActiva ? "" : " Activa «Aplicar perfiles automáticamente» para que se use solo.")
            : "No se pudo guardar el perfil.", ok ? InfoBarSeverity.Success : InfoBarSeverity.Error);
    }

    private void Aviso(string texto, InfoBarSeverity tipo)
    {
        Estado.Severity = tipo;
        Estado.Message = texto;
        Estado.IsOpen = true;
    }
}
