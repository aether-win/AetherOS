using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CentroAether.Controles;
using CentroAether.Services;

namespace CentroAether.Pages;

public sealed partial class ModosPage : PaginaBase
{
    private string _modoActual = "";
    private bool _ocupado;

    private static readonly (string id, string titulo, string desc, string[] puntos)[] Modos =
    {
        ("Trabajo", "Trabajo", "Equilibrado para el día a día.",
            new[] { "Todo activo y notificaciones", "Turbo del procesador disponible", "Wi-Fi a máximo rendimiento con cargador", "Modo Juego de Windows listo" }),
        ("Gaming", "Gaming", "Máximo rendimiento para jugar.",
            new[] { "Solo con el cargador conectado", "Sin estacionar núcleos ni segundo plano", "Libera la RAM en espera al entrar", "Modo Juego y prioridad alta de GPU para juegos" }),
        ("Bateria", "Batería", "Eficiente para durar más.",
            new[] { "Ahorro de batería de Windows", "Procesador eficiente, sin turbo", "Wi-Fi en ahorro medio" }),
    };

    public ModosPage()
    {
        InitializeComponent();
    }

    private bool _cargandoNivel;
    private static readonly string[] Niveles = { "Gaming", "Extremo", "Maximo" };

    // Cada optimización: (nivel mínimo, nombre, beneficio, justificación, riesgo). Todas reversibles.
    private static readonly (int Nivel, string Nombre, string Beneficio, string Porque, string Riesgo)[] Optimizaciones =
    {
        (0, "CPU al 100% sin estacionar núcleos (EPP 0)", "Frecuencia estable, sin bajones al jugar.", "El procesador no baja de velocidad ni apaga núcleos en mitad de la partida.", "Bajo: más consumo; solo con cargador."),
        (0, "Modo de energía \"Mejor rendimiento\" y temporizador 0,5 ms", "Respuesta y ritmo de fotogramas más parejos.", "Windows despierta los hilos del juego con más precisión.", "Bajo."),
        (0, "Sin notificaciones ni apps en segundo plano", "Menos interrupciones y menos uso de CPU/RAM.", "Libera recursos que el juego aprovecha.", "Ninguno: se restaura al salir."),
        (0, "Memoria en espera liberada al entrar", "El juego encuentra RAM libre al instante.", "Se vacía la caché vieja de Windows (como RAMMap de Microsoft).", "Ninguno: Windows la vuelve a llenar sola."),
        (0, "Modo Juego de Windows y prioridad MMCSS \"Games\" (GPU 8, alta)", "Fotogramas más estables con otras apps abiertas.", "Windows da preferencia de CPU y GPU al juego en primer plano.", "Ninguno."),
        (0, "Wi-Fi a máximo rendimiento", "Menos ping y sin cortes de la red inalámbrica.", "La tarjeta Wi-Fi no entra en ahorro de energía.", "Bajo: algo más de consumo con cargador."),
        (1, "Sin suspensión selectiva USB", "Mando, ratón y teclado sin retrasos al \"despertar\".", "Windows no duerme los puertos USB entre pulsaciones.", "Bajo: algo más de consumo."),
        (1, "Sin ahorro de energía PCI Express (ASPM)", "Menos latencia de la gráfica y el SSD.", "El enlace PCIe no entra en reposo.", "Bajo: algo más de consumo y calor."),
        (1, "Búsqueda de Windows en pausa", "Sin picos de disco/CPU del indexado mientras juegas.", "El indexador se detiene y vuelve al salir de Gaming.", "Ninguno: la búsqueda vuelve al salir."),
        (1, "Sin recorte de energía a las apps (Power Throttling)", "El juego nunca queda en \"modo eficiente\".", "Windows no limita procesos que cree en segundo plano.", "Bajo."),
        (2, "CPU sin estados de reposo + turbo agresivo siempre", "La menor latencia posible.", "El procesador nunca entra en reposo profundo.", "MEDIO: bastante más calor; úsalo con ventilador."),
    };

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Construir();
        _cargandoNivel = true;
        CmbNivel.SelectedIndex = Math.Max(0, Array.IndexOf(Niveles, Aether.NivelGaming));
        _cargandoNivel = false;
        PintarNivel();
        await RefrescarModo();
    }

    private void PintarNivel()
    {
        int nivel = Math.Max(0, CmbNivel.SelectedIndex);
        FilaNivel.Descripcion = nivel switch
        {
            1 => "Gaming Extremo: Gaming + menos latencia de USB y PCIe, sin indexado ni recorte de energía. Todo se deshace al salir de Gaming.",
            2 => "Máximo Rendimiento: Extremo + CPU sin reposo y turbo agresivo. Calienta bastante más: úsalo con ventilador.",
            _ => "Gaming: máximo rendimiento del procesador, sin distracciones. Lo recomendado para el día a día.",
        };
        ListaOptimizaciones.Children.Clear();
        foreach (var o in Optimizaciones)
        {
            bool incluida = o.Nivel <= nivel;
            ListaOptimizaciones.Children.Add(new FilaAjuste
            {
                Glyph = incluida ? "\uE73E" : "\uE711",
                ColorIcono = Ui.Recurso(incluida ? "SystemFillColorSuccessBrush" : "TextFillColorTertiaryBrush"),
                Titulo = o.Nombre,
                Descripcion = $"{o.Beneficio} Por qué: {o.Porque}",
                Nota = $"Riesgo: {o.Riesgo} · Reversible al salir de Gaming",
                ColorNota = Ui.Recurso(o.Riesgo.StartsWith("MEDIO") ? "SystemFillColorCautionBrush" : "TextFillColorTertiaryBrush"),
                Opacity = incluida ? 1.0 : 0.55,
                MinHeight = 0,
            });
        }
    }

    private async void Nivel_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_cargandoNivel || CmbNivel.SelectedIndex < 0)
        {
            return;
        }
        Aether.NivelGaming = Niveles[CmbNivel.SelectedIndex];
        PintarNivel();
        if (string.Equals(Ui.NombreModo(_modoActual), "Gaming", StringComparison.OrdinalIgnoreCase))
        {
            // Ya estás en Gaming: re-aplicar con el nuevo nivel (deshace el anterior primero).
            await Aplicar("Gaming");
        }
        else
        {
            Estado.Severity = InfoBarSeverity.Success;
            Estado.Message = "Nivel guardado: se usará la próxima vez que actives el Modo Gaming.";
            Estado.IsOpen = true;
        }
    }

    private async Task RefrescarModo()
    {
        if (!Aether.Disponible)
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = "No se encontró Aether.Centro.ps1 en este equipo.";
            Estado.IsOpen = true;
            return;
        }
        _modoActual = await Aether.ModoActual();
        Construir();
    }

    private void Construir()
    {
        GridModos.Children.Clear();
        foreach (var m in Modos)
        {
            GridModos.Children.Add(CrearTarjeta(m.id, m.titulo, m.desc, m.puntos));
        }
    }

    private Button CrearTarjeta(string id, string titulo, string desc, string[] puntos)
    {
        bool activo = string.Equals(Ui.NombreModo(_modoActual), Ui.NombreModo(id), StringComparison.OrdinalIgnoreCase);
        Color color = Ui.ColorModo(id);

        var cabecera = new Grid();
        cabecera.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        cabecera.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cabecera.Children.Add(Ui.CajaIcono(Ui.GlyphModo(id), color, 52, 24));
        if (activo)
        {
            Border insignia = Ui.Insignia("Activo", color);
            insignia.HorizontalAlignment = HorizontalAlignment.Right;
            insignia.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(insignia, 1);
            cabecera.Children.Add(insignia);
        }

        var contenido = new Pila { Spacing = 12 };
        contenido.Children.Add(cabecera);
        var textos = new Pila { Spacing = 2 };
        textos.Children.Add(new TextBlock { Text = titulo, FontSize = 20, FontWeight = FontWeights.SemiBold });
        textos.Children.Add(new TextBlock { Text = desc, FontSize = 13, TextWrapping = TextWrapping.Wrap, Foreground = Ui.Recurso("TextFillColorSecondaryBrush") });
        contenido.Children.Add(textos);

        var lista = new Pila { Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };
        foreach (string p in puntos)
        {
            // Grid (no Pila horizontal) para que el texto largo baje de línea en vez de cortarse.
            var fila = new Grid();
            fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var check = new FontIcon { Glyph = "\uE73E", FontSize = 12, Foreground = new SolidColorBrush(color), VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 3, 8, 0) };
            var texto = new TextBlock { Text = p, FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Foreground = Ui.Recurso("TextFillColorSecondaryBrush") };
            Grid.SetColumn(texto, 1);
            fila.Children.Add(check);
            fila.Children.Add(texto);
            lista.Children.Add(fila);
        }
        contenido.Children.Add(lista);
        contenido.Children.Add(new TextBlock
        {
            Text = activo ? "Este es tu modo actual" : "Clic para activar",
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0),
            Foreground = activo ? new SolidColorBrush(color) : Ui.Recurso("AccentTextFillColorPrimaryBrush"),
        });

        var b = new Button
        {
            Content = contenido,
            Style = (Style)FindResource("MosaicoStyle"),
            Padding = new Thickness(20, 20, 20, 18),
        };
        if (activo)
        {
            b.BorderBrush = new SolidColorBrush(color);
            b.BorderThickness = new Thickness(2);
        }
        b.Click += async (_, _) => await Aplicar(id);
        return b;
    }

    private async Task Aplicar(string modo)
    {
        if (_ocupado)
        {
            return;
        }
        if (!Aether.Disponible)
        {
            // Sin Aether OS instalado no hay modos que cambiar: no mostrar un "activo" falso.
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = "Aether OS no está instalado en este equipo, así que no se puede cambiar de modo. Puedes ver la animación con el botón de arriba.";
            Estado.IsOpen = true;
            return;
        }
        // "Cargando modo": se bloquean las tarjetas y se avisa hasta que quede estable.
        _ocupado = true;
        GridModos.IsEnabled = false;
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = $"Cargando modo {Ui.NombreModo(modo)}…";
        Estado.IsOpen = true;

        (bool ok, string mensaje) = await Aether.CambiarModoDetallado(modo);

        await RefrescarModo();
        GridModos.IsEnabled = true;
        _ocupado = false;

        if (!ok)
        {
            // P. ej. "Conecta el cargador para usar el Modo Gaming."
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = mensaje;
        }
        else
        {
            Estado.Severity = InfoBarSeverity.Success;
            Estado.Message = $"Modo {Ui.NombreModo(modo)} activo.";
            // Cada modo muestra su propio overlay (Gaming, Trabajo o Batería).
            VentanaOverlayModo.Mostrar(modo);
        }
        Estado.IsOpen = true;
        SegundoPlano.ModoCambiado(_modoActual);
    }

    private void VistaPreviaOverlay_Click(object sender, RoutedEventArgs e)
        => VentanaOverlayModo.Mostrar(string.IsNullOrEmpty(_modoActual) ? "Trabajo" : _modoActual);
}
