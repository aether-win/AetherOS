using System;
using CentroAether.Models;
using CentroAether.Pages;
using CentroAether.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace CentroAether;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "Centro Aether";
        this.ExtendsContentIntoTitleBar = false;
        if (this.AppWindow?.Presenter is Microsoft.UI.Windowing.OverlappedPresenter)
        {
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1160, 780));
        }

        ConstruirMenu();
    }

    /// <summary>
    /// Construye el menú lateral a partir del registro central de módulos,
    /// insertando un encabezado cada vez que cambia la categoría.
    /// </summary>
    private void ConstruirMenu()
    {
        string? categoriaActual = null;

        foreach (Modulo m in RegistroModulos.Todos)
        {
            if (!string.IsNullOrEmpty(m.Categoria) && m.Categoria != categoriaActual)
            {
                categoriaActual = m.Categoria;
                Nav.MenuItems.Add(new NavigationViewItemHeader { Content = categoriaActual });
            }

            var item = new NavigationViewItem
            {
                Content = m.Nombre,
                Tag = m.Id,
                Icon = new FontIcon { Glyph = m.Glyph },
            };

            if (!m.Disponible)
            {
                ToolTipService.SetToolTip(item, "Próximamente");
                item.Opacity = 0.75;
            }

            Nav.MenuItems.Add(item);
        }
    }

    private void Nav_Loaded(object sender, RoutedEventArgs e)
    {
        if (Nav.MenuItems.Count > 0)
        {
            Nav.SelectedItem = Nav.MenuItems[0];
            Navegar("inicio");
        }
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            Navegar(tag);
        }
    }

    private void Navegar(string id)
    {
        Modulo? m = RegistroModulos.Buscar(id);
        if (m is null)
        {
            return;
        }

        Type pagina = m.Pagina ?? typeof(ProximamentePage);
        object parametro = m.Pagina is null ? m : id;

        // Las páginas "Próximamente" comparten tipo, así que siempre navegamos
        // para que el contenido del módulo seleccionado se actualice.
        if (ContentFrame.CurrentSourcePageType != pagina || pagina == typeof(ProximamentePage))
        {
            ContentFrame.Navigate(pagina, parametro, new DrillInNavigationTransitionInfo());
        }
    }
}
