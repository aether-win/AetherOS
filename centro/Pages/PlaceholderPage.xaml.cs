using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

public sealed partial class PlaceholderPage : Page
{
    private static readonly Dictionary<string, string> Titulos = new()
    {
        ["inicio"] = "Aether",
        ["red"] = "Red y carpetas compartidas",
        ["actualizar"] = "Actualizaciones",
        ["limpieza"] = "Limpieza",
        ["bateria"] = "Bateria",
        ["respaldo"] = "Respaldo",
        ["wind"] = "Wind-Aether",
        ["salud"] = "Salud",
    };

    public PlaceholderPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string tag && Titulos.TryGetValue(tag, out string? titulo))
        {
            TxtTitulo.Text = titulo;
        }
    }
}
