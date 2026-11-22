using System;
using System.Windows;
using System.Windows.Controls;
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
        Tema.BarraOscura(this);
        ConstruirMenu();

        Loaded += (_, _) => SeleccionarPrimero();

        // Al cerrar el Centro, detener el motor del controlador y la página activa.
        Closed += (_, _) =>
        {
            _actual?.Salir();
            MotorControlador.Detener();
        };
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
                    Content = new TextBlock { Text = categoriaActual, FontSize = 12, FontWeight = FontWeights.SemiBold },
                });
            }

            var fila = new Pila { Orientation = Orientation.Horizontal, Spacing = 12 };
            fila.Children.Add(new FontIcon { Glyph = m.Glyph, FontSize = 16 });
            fila.Children.Add(new TextBlock { Text = m.Nombre, VerticalAlignment = VerticalAlignment.Center });

            var item = new ListBoxItem { Style = estiloItem, Content = fila, Tag = m.Id };
            if (!m.Disponible)
            {
                item.ToolTip = "Próximamente";
                item.Opacity = 0.75;
            }
            Nav.Items.Add(item);
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
