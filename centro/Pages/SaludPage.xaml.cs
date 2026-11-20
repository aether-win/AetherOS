using System;
using CentroAether.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CentroAether.Pages;

public sealed partial class SaludPage : Page
{
    public SaludPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        string s = (await Aether.SaludBateria()).Trim();

        if (s == "SIN" || !s.Contains('|'))
        {
            TxtSalud.Text = "Sin datos";
            TxtDetalle.Text = "No se pudo leer la salud de la bateria (puede que esta PC no tenga bateria o el driver no la reporte).";
            BarSalud.Value = 0;
            return;
        }

        string[] p = s.Split('|');
        if (p.Length >= 3 && int.TryParse(p[0], out int salud)
            && int.TryParse(p[1], out int actual) && int.TryParse(p[2], out int diseno))
        {
            TxtSalud.Text = $"{salud}% de salud";
            TxtDetalle.Text = $"Capacidad actual: {actual} mWh de {diseno} mWh de fabrica. "
                + (salud >= 80 ? "La bateria esta en buen estado."
                   : salud >= 60 ? "La bateria ya perdio algo de capacidad."
                   : "La bateria esta bastante desgastada; considera reemplazarla.");
            BarSalud.Value = salud;
        }
        else
        {
            TxtSalud.Text = "Sin datos";
            TxtDetalle.Text = "No se pudo interpretar la salud de la bateria.";
        }
    }
}
