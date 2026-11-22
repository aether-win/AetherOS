using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CentroAether.Controles;

namespace CentroAether;

/// <summary>Un programa abierto, listo para crear un perfil sin escribir nada a mano.</summary>
public sealed class ProcesoInfo
{
    public string Nombre { get; init; } = "";     // nombre legible ("Grand Theft Auto V")
    public string Ejecutable { get; init; } = ""; // "GTA5.exe"
    public string Ruta { get; init; } = "";       // ruta completa, si se puede leer
    public string Titulo { get; init; } = "";     // título de su ventana
    public bool TieneVentana { get; init; }
}

/// <summary>
/// Ventana para elegir un proceso abierto: muestra nombre de la app, ejecutable, ruta y título,
/// con búsqueda. Solo procesos del usuario (no los de Windows en segundo plano), sin duplicados.
/// </summary>
public partial class VentanaProcesos : Window
{
    private List<ProcesoInfo> _todos = new();

    public ProcesoInfo? Elegido { get; private set; }

    public VentanaProcesos()
    {
        InitializeComponent();
        Tema.Mica(this);
        Loaded += async (_, _) => await Cargar();
    }

    private async Task Cargar()
    {
        TxtCargando.Visibility = Visibility.Visible;
        _todos = await Task.Run(Leer);
        TxtCargando.Visibility = Visibility.Collapsed;
        Pintar();
        TxtBuscar.Focus();
    }

    private static List<ProcesoInfo> Leer()
    {
        int sesion = Process.GetCurrentProcess().SessionId;
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var lista = new Dictionary<string, ProcesoInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (Process p in Process.GetProcesses())
        {
            using (p)
            {
                try
                {
                    if (p.SessionId != sesion || p.Id == Environment.ProcessId)
                    {
                        continue;
                    }
                    string ruta = "";
                    try { ruta = p.MainModule?.FileName ?? ""; } catch { /* sin acceso */ }
                    if (ruta.StartsWith(windows, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;   // componentes de Windows: no son juegos ni apps
                    }
                    string exe = ruta.Length > 0 ? Path.GetFileName(ruta) : p.ProcessName + ".exe";
                    string descripcion = "";
                    try { descripcion = ruta.Length > 0 ? FileVersionInfo.GetVersionInfo(ruta).FileDescription ?? "" : ""; } catch { /* sin datos */ }
                    string titulo = "";
                    try { titulo = p.MainWindowTitle ?? ""; } catch { /* sin ventana */ }
                    bool ventana = titulo.Length > 0;

                    string nombre = !string.IsNullOrWhiteSpace(descripcion) ? descripcion.Trim()
                        : ventana ? titulo : p.ProcessName;

                    if (lista.TryGetValue(exe, out ProcesoInfo? ya) && (ya.TieneVentana || !ventana))
                    {
                        continue;   // mismo .exe repetido (navegadores, juegos con varios procesos)
                    }
                    lista[exe] = new ProcesoInfo { Nombre = nombre, Ejecutable = exe, Ruta = ruta, Titulo = titulo, TieneVentana = ventana };
                }
                catch { /* proceso que se cerró mientras se leía */ }
            }
        }
        return lista.Values.OrderByDescending(x => x.TieneVentana).ThenBy(x => x.Nombre, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private void Pintar()
    {
        string filtro = TxtBuscar.Text.Trim();
        bool todos = ChkTodos.IsChecked == true;
        Lista.Items.Clear();
        foreach (ProcesoInfo p in _todos)
        {
            if (!todos && !p.TieneVentana)
            {
                continue;
            }
            if (filtro.Length > 0
                && p.Nombre.IndexOf(filtro, StringComparison.CurrentCultureIgnoreCase) < 0
                && p.Ejecutable.IndexOf(filtro, StringComparison.OrdinalIgnoreCase) < 0
                && p.Titulo.IndexOf(filtro, StringComparison.CurrentCultureIgnoreCase) < 0)
            {
                continue;
            }
            Lista.Items.Add(new ListBoxItem { Content = Fila(p), Tag = p });
        }
        TxtCargando.Text = "No hay programas que coincidan.";
        TxtCargando.Visibility = Lista.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static UIElement Fila(ProcesoInfo p)
    {
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var icono = new Border
        {
            Width = 34,
            Height = 34,
            CornerRadius = new CornerRadius(9),
            Background = Ui.Recurso("IconoFondoBrush"),
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new FontIcon { Glyph = p.TieneVentana ? "" : "", FontSize = 15, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Ui.Recurso("AccentTextFillColorPrimaryBrush") },
        };
        g.Children.Add(icono);
        var textos = new Pila();
        textos.Children.Add(new TextBlock { Text = p.Nombre, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
        textos.Children.Add(new TextBlock
        {
            Text = p.Ejecutable + (p.Ruta.Length > 0 ? "  ·  " + p.Ruta : ""),
            FontSize = 12,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = Ui.Recurso("TextFillColorSecondaryBrush"),
        });
        Grid.SetColumn(textos, 1);
        g.Children.Add(textos);
        return g;
    }

    private void Buscar_Changed(object sender, TextChangedEventArgs e) => Pintar();

    private void Todos_Changed(object sender, RoutedEventArgs e) => Pintar();

    private void Lista_Seleccion(object sender, SelectionChangedEventArgs e) => BtnElegir.IsEnabled = Lista.SelectedItem is not null;

    private void Lista_DobleClic(object sender, MouseButtonEventArgs e) => Elegir();

    private void Elegir_Click(object sender, RoutedEventArgs e) => Elegir();

    private void Elegir()
    {
        if (Lista.SelectedItem is ListBoxItem { Tag: ProcesoInfo p })
        {
            Elegido = p;
            DialogResult = true;
        }
    }
}
