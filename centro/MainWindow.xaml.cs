using System;
using CentroAether.Pages;
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
        if (this.AppWindow?.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
        {
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1120, 760));
        }
    }

    private void Nav_Loaded(object sender, RoutedEventArgs e)
    {
        Nav.SelectedItem = Nav.MenuItems[0];
        Navegar("inicio");
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            Navegar(tag);
        }
    }

    private void Navegar(string tag)
    {
        Type pagina = tag switch
        {
            "inicio" => typeof(InicioPage),
            "modos" => typeof(ModosPage),
            "red" => typeof(RedPage),
            "actualizar" => typeof(ActualizarPage),
            "limpieza" => typeof(LimpiezaPage),
            "bateria" => typeof(BateriaPage),
            "respaldo" => typeof(RespaldoPage),
            "wind" => typeof(WindPage),
            "salud" => typeof(SaludPage),
            _ => typeof(PlaceholderPage),
        };

        if (ContentFrame.CurrentSourcePageType != pagina || pagina == typeof(PlaceholderPage))
        {
            ContentFrame.Navigate(pagina, tag, new DrillInNavigationTransitionInfo());
        }
    }
}
