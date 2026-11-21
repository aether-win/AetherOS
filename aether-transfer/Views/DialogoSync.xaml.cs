using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AetherTransfer.Models;

namespace AetherTransfer.Views;

/// <summary>Una fila de la comparación de sincronización.</summary>
public sealed class FilaSync
{
    public string Nombre { get; init; } = "";
    public string Estado { get; init; } = "";
    public bool SoloLocal { get; init; }
    public bool SoloRemoto { get; init; }
    public bool LocalNuevo { get; init; }
    public bool RemotoNuevo { get; init; }
}

/// <summary>Una acción de sincronización a encolar.</summary>
public sealed class AccionSync
{
    public string Nombre { get; set; } = "";
    public bool Subida { get; set; }
    public long Tamano { get; set; }
}

public partial class DialogoSync : Window
{
    private readonly ObservableCollection<FilaSync> _filas = new();
    private readonly Dictionary<string, long> _tam = new();

    /// <summary>Acciones elegidas tras pulsar Sincronizar (según la dirección).</summary>
    public List<AccionSync> Acciones { get; } = new();

    public DialogoSync(IReadOnlyList<ElementoRemoto> locales, IReadOnlyList<ElementoRemoto> remotos)
    {
        InitializeComponent();

        var loc = locales.Where(x => !x.EsCarpeta && x.Nombre != "..").ToDictionary(x => x.Nombre, x => x);
        var rem = remotos.Where(x => !x.EsCarpeta && x.Nombre != "..").ToDictionary(x => x.Nombre, x => x);

        foreach (string nombre in loc.Keys.Union(rem.Keys).OrderBy(n => n, System.StringComparer.CurrentCultureIgnoreCase))
        {
            bool enLoc = loc.TryGetValue(nombre, out ElementoRemoto? l);
            bool enRem = rem.TryGetValue(nombre, out ElementoRemoto? r);
            _tam[nombre] = (enLoc ? l!.Tamano : 0) is long t && t > 0 ? t : (enRem ? r!.Tamano : 0);

            FilaSync fila;
            if (enLoc && !enRem)
            {
                fila = new FilaSync { Nombre = nombre, Estado = "Solo en el equipo", SoloLocal = true };
            }
            else if (!enLoc && enRem)
            {
                fila = new FilaSync { Nombre = nombre, Estado = "Solo en el servidor", SoloRemoto = true };
            }
            else
            {
                int cmp = l!.Modificado.CompareTo(r!.Modificado);
                if (cmp > 0)
                {
                    fila = new FilaSync { Nombre = nombre, Estado = "El equipo es más reciente", LocalNuevo = true };
                }
                else if (cmp < 0)
                {
                    fila = new FilaSync { Nombre = nombre, Estado = "El servidor es más reciente", RemotoNuevo = true };
                }
                else
                {
                    fila = new FilaSync { Nombre = nombre, Estado = "Igual" };
                }
            }
            _filas.Add(fila);
        }

        ListaSync.ItemsSource = _filas;
        Resumen();
    }

    private void Sincronizar_Click(object sender, RoutedEventArgs e)
    {
        RecogerAcciones();
        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Direccion_Changed(object sender, SelectionChangedEventArgs e) => Resumen();

    private bool EsSubir => CmbDireccion.SelectedIndex == 0;

    private void Resumen()
    {
        if (TxtResumen is null)
        {
            return;
        }
        int n = _filas.Count(f => EsSubir ? (f.SoloLocal || f.LocalNuevo) : (f.SoloRemoto || f.RemotoNuevo));
        TxtResumen.Text = EsSubir
            ? $"Se subirán {n} archivo(s) al servidor."
            : $"Se descargarán {n} archivo(s) al equipo.";
    }

    private void RecogerAcciones()
    {
        Acciones.Clear();
        foreach (FilaSync f in _filas)
        {
            bool actuar = EsSubir ? (f.SoloLocal || f.LocalNuevo) : (f.SoloRemoto || f.RemotoNuevo);
            if (actuar)
            {
                Acciones.Add(new AccionSync { Nombre = f.Nombre, Subida = EsSubir, Tamano = _tam.TryGetValue(f.Nombre, out long t) ? t : 0 });
            }
        }
    }
}
