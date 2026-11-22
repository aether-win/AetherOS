using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CentroAether.Controles;
using CentroAether.Models;
using CentroAether.Pages;
using CentroAether.Services;

namespace CentroAether;

public partial class MainWindow : Window
{
    private PaginaBase? _actual;

    public MainWindow()
    {
        InitializeComponent();
        Tema.Mica(this);
        ConstruirMenu();

        Loaded += (_, _) =>
        {
            SeleccionarPrimero();
            _ = ActualizarModoMenu();
            ActualizarMandoMenu();
        };

        // Cerrar la ventana solo la oculta: el Centro sigue en segundo plano (bandeja, atajos,
        // mando y modos). El mando emulado NO se apaga al cerrar.
        Closing += (_, e) =>
        {
            e.Cancel = true;
            _actual?.Salir();
            _actual = null;
            Hide();
        };
    }

    /// <summary>Muestra la ventana (desde la bandeja, el acceso directo o una segunda copia).</summary>
    public void MostrarDesdeSegundoPlano()
    {
        bool estabaOculta = !IsVisible;
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();
        Topmost = true;    // truco para traerla al frente aunque otra app tenga el foco
        Topmost = false;
        Focus();

        // Al volver de estar oculta, recargar la sección que estaba abierta.
        if (estabaOculta && _actual is null && Nav.SelectedItem is ListBoxItem li && li.Tag is string id)
        {
            Navegar(id);
        }
        _ = ActualizarModoMenu();
    }

    /// <summary>
    /// Construye el menú lateral a partir del registro central de módulos,
    /// insertando un encabezado cada vez que cambia la categoría.
    /// </summary>
    private void ConstruirMenu()
    {
        string? categoriaActual = null;
        var estiloItem = (Style)FindResource("ItemMenu");
        var estiloEncabezado = (Style)FindResource("EncabezadoMenu");

        foreach (Modulo m in RegistroModulos.Todos)
        {
            if (!string.IsNullOrEmpty(m.Categoria) && m.Categoria != categoriaActual)
            {
                categoriaActual = m.Categoria;
                Nav.Items.Add(new ListBoxItem
                {
                    Style = estiloEncabezado,
                    Content = new TextBlock { Text = categoriaActual.ToUpperInvariant(), FontSize = 11, FontWeight = FontWeights.SemiBold },
                });
            }

            var fila = new Pila { Orientation = Orientation.Horizontal, Spacing = 14 };
            var icono = new FontIcon { Glyph = m.Glyph, FontSize = 16, Width = 20 };
            icono.SetResourceReference(TextBlock.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
            fila.Children.Add(icono);
            fila.Children.Add(new TextBlock { Text = m.Nombre, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });

            var item = new ListBoxItem { Style = estiloItem, Content = fila, Tag = m.Id };
            if (!m.Disponible)
            {
                item.ToolTip = "Próximamente";
                item.Opacity = 0.75;
            }
            Nav.Items.Add(item);
        }
    }

    /// <summary>Abre una sección desde otra (p. ej. los accesos rápidos de Inicio).</summary>
    public void IrA(string id)
    {
        foreach (object o in Nav.Items)
        {
            if (o is ListBoxItem li && li.Tag as string == id)
            {
                Nav.SelectedItem = li;
                li.BringIntoView();
                return;
            }
        }
    }

    /// <summary>Indicador del mando en el menú lateral (tiempo real, por eventos del servicio).</summary>
    public void ActualizarMandoMenu()
    {
        if (!ServicioMandos.Activo)
        {
            PuntoMando.Fill = new SolidColorBrush(Ui.Hex("#3A4A60"));
            TxtMandoTitulo.Text = "Mandos";
            TxtMandoMenu.Text = "Desactivados";
            return;
        }
        MandoConectado? m = ServicioMandos.Conectados.FirstOrDefault();
        bool hay = m is not null;
        PuntoMando.Fill = new SolidColorBrush(Ui.Hex(hay ? "#3FBF7F" : "#6F87A3"));
        TxtMandoTitulo.Text = hay ? $"Mando conectado · {m!.TextoConexion}" : "Mando";
        TxtMandoMenu.Text = hay ? m!.Nombre : "Desconectado";
    }

    private void CajaMando_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => IrA("control");

    /// <summary>Refresca el modo que se muestra en el pie del menú (con su color).</summary>
    public async Task ActualizarModoMenu()
    {
        if (!Aether.Disponible)
        {
            TxtModoMenu.Text = "Sin instalar";
            return;
        }
        try
        {
            string modo = await Aether.ModoActual();
            TxtModoMenu.Text = Ui.NombreModo(modo);
            PuntoModo.Fill = new SolidColorBrush(Ui.ColorModo(modo));
        }
        catch
        {
            TxtModoMenu.Text = "—";
        }
    }

    private void SeleccionarPrimero()
    {
        foreach (object o in Nav.Items)
        {
            if (o is ListBoxItem li && li.Tag is string)
            {
                Nav.SelectedItem = li;   // dispara Nav_SelectionChanged → Navegar
                return;
            }
        }
    }

    private void Nav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Nav.SelectedItem is ListBoxItem item && item.Tag is string id)
        {
            Navegar(id);
        }
    }

    private void Navegar(string id)
    {
        Modulo? m = RegistroModulos.Buscar(id);
        if (m is null)
        {
            return;
        }

        Type tipo = m.Pagina ?? typeof(ProximamentePage);
        object parametro = m.Pagina is null ? m : id;

        PaginaBase pagina;
        try
        {
            pagina = (PaginaBase)Activator.CreateInstance(tipo)!;
        }
        catch (Exception ex)
        {
            App.Registrar($"Abrir la sección {m.Nombre}", ex);
            Contenido.Content = new TextBlock
            {
                Text = $"No se pudo abrir «{m.Nombre}»: {ex.GetBaseException().Message}",
                Margin = new Thickness(32),
                TextWrapping = TextWrapping.Wrap,
            };
            return;
        }

        _actual?.Salir();
        _actual = pagina;
        Contenido.Content = pagina;
        pagina.Entrar(parametro);

        // Transición suave al cambiar de sección.
        var aparecer = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
        pagina.BeginAnimation(OpacityProperty, aparecer);
    }
}
