using System;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CentroAether.Controles;
using CentroAether.Services;

namespace CentroAether.Pages;

/// <summary>
/// Salud en vivo: la batería con todos los datos del informe de Windows (capacidad de fábrica,
/// capacidad actual, desgaste, ciclos, voltaje, consumo) y el estado de los discos (SMART y
/// desgaste de SSD). Se actualiza sola; Aether Toolbox corre con permisos de administrador,
/// que son los que Windows pide para leer estos datos.
/// </summary>
public sealed partial class SaludPage : PaginaBase
{
    private readonly DispatcherTimer _vivo = new() { Interval = TimeSpan.FromSeconds(3) };
    private bool _discosLeidos;

    public SaludPage()
    {
        this.InitializeComponent();
        _vivo.Tick += async (_, _) => await Pintar();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await Pintar();
        _vivo.Start();
        if (!_discosLeidos)
        {
            _discosLeidos = true;
            await PintarDiscos();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _vivo.Stop();
    }

    private async Task Pintar()
    {
        EstadoBateria b = await Task.Run(BateriaVivo.Leer);
        if (!b.Hay)
        {
            TxtSalud.Text = "Sin batería";
            TxtDetalle.Text = "Esta PC no tiene batería o el driver no la reporta.";
            BarSalud.Value = 0;
            InfoBateria.Visibility = Visibility.Collapsed;
            return;
        }
        if (b.SaludPct > 0)
        {
            TxtSalud.Text = $"{b.SaludPct}% de salud";
            BarSalud.Value = Math.Min(100, b.SaludPct);
            TxtDetalle.Text = (b.SaludPct >= 80 ? "La batería está en buen estado."
                : b.SaludPct >= 60 ? "La batería ya perdió algo de capacidad."
                : "La batería está bastante desgastada; considera reemplazarla.")
                + $" Desgaste: {Math.Max(0, 100 - b.SaludPct)}%.";
        }
        else
        {
            TxtSalud.Text = $"{b.Porcentaje}% de carga";
            BarSalud.Value = b.Porcentaje;
            TxtDetalle.Text = "El driver no informa la capacidad de fábrica, así que no se puede calcular el desgaste.";
        }
        InfoBateria.Poner("Capacidad de fábrica", b.DisenoMwh > 0 ? $"{b.DisenoMwh / 1000.0:0.0} Wh" : "—");
        InfoBateria.Poner("Capacidad actual (llena)", b.CompletaMwh > 0 ? $"{b.CompletaMwh / 1000.0:0.0} Wh" : "—");
        InfoBateria.Poner("Ciclos de carga", b.Ciclos >= 0 ? b.Ciclos.ToString() : "—");
        InfoBateria.Poner("Carga ahora", $"{b.Porcentaje}%");
        InfoBateria.Poner(b.Cargando ? "Carga entrando" : "Consumo ahora", BateriaVivo.Tasa(b.TasaMw));
        InfoBateria.Poner(b.Cargando ? "Para llenarse" : "Tiempo restante", BateriaVivo.Tiempo(b.MinutosRestantes));
        InfoBateria.Poner("Voltaje", b.VoltajeMv > 0 ? $"{b.VoltajeMv / 1000.0:0.00} V" : "—");
        InfoBateria.Poner("Fabricante", string.IsNullOrEmpty(b.Fabricante) ? "—" : b.Fabricante);
        InfoBateria.Poner("Modelo", string.IsNullOrEmpty(b.Modelo) ? "—" : b.Modelo);
    }

    private async Task PintarDiscos()
    {
        var discos = await Task.Run(() =>
        {
            var lista = new System.Collections.Generic.List<(string Nombre, string Tipo, string Salud, string Desgaste, string Temp, string Tamano)>();
            try
            {
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
                using var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM MSFT_PhysicalDisk"));
                foreach (ManagementObject d in s.Get())
                {
                    string nombre = (d["FriendlyName"] as string ?? "Disco").Trim();
                    string tipo = Convert.ToInt32(d["MediaType"] ?? 0) switch { 4 => "SSD", 3 => "Disco duro", 5 => "SCM", _ => "Disco" };
                    string salud = Convert.ToInt32(d["HealthStatus"] ?? 0) switch { 0 => "Bien", 1 => "Atención", 2 => "Mal", _ => "Desconocido" };
                    double gb = Convert.ToDouble(d["Size"] ?? 0) / 1e9;
                    string desgaste = "—", temp = "—";
                    try
                    {
                        string id = d["DeviceId"]?.ToString() ?? "";
                        using var r = new ManagementObjectSearcher(scope, new ObjectQuery(
                            "ASSOCIATORS OF {MSFT_PhysicalDisk.ObjectId='" + (d["ObjectId"]?.ToString() ?? "").Replace("\\", "\\\\").Replace("'", "\\'") +
                            "'} WHERE AssocClass = MSFT_PhysicalDiskToStorageReliabilityCounter"));
                        foreach (ManagementObject c in r.Get())
                        {
                            if (c["Wear"] is not null) { desgaste = $"{Convert.ToInt32(c["Wear"])}%"; }
                            if (c["Temperature"] is not null && Convert.ToInt32(c["Temperature"]) > 0) { temp = $"{Convert.ToInt32(c["Temperature"])} °C"; }
                        }
                    }
                    catch { /* el disco no expone contadores de fiabilidad */ }
                    lista.Add((nombre, tipo, salud, desgaste, temp, gb >= 1000 ? $"{gb / 1000:0.0} TB" : $"{gb:0} GB"));
                }
            }
            catch { /* sin acceso al proveedor de almacenamiento */ }
            return lista;
        });

        PanelDiscos.Children.Clear();
        if (discos.Count == 0)
        {
            PanelDiscos.Children.Add(new System.Windows.Controls.TextBlock { Text = "No se pudo leer el estado de los discos.", Margin = new Thickness(4) });
            return;
        }
        foreach (var d in discos)
        {
            var est = new Estadisticas();
            est.Poner("Disco", $"{d.Nombre}");
            est.Poner("Tipo y tamaño", $"{d.Tipo} · {d.Tamano}");
            est.Poner("Salud", d.Salud);
            est.Poner("Desgaste", d.Desgaste);
            est.Poner("Temperatura", d.Temp);
            PanelDiscos.Children.Add(est);
        }
    }
}
