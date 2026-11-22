using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CentroAether.Services;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using CentroAether.Controles;

namespace CentroAether.Pages;

/// <summary>
/// Live Monitor: uso de CPU y RAM en tiempo real usando APIs nativas de Windows
/// (GetSystemTimes y GlobalMemoryStatusEx), sin dependencias externas. La GPU y
/// las temperaturas se añadirán en una fase posterior.
/// </summary>
public sealed partial class MonitorPage : PaginaBase
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private ulong _prevIdle, _prevKernel, _prevUser;
    private bool _tieneBase;
    private int _tick;
    private MonitorVivo? _vivo;
    private bool _muestreando;

    public MonitorPage()
    {
        this.InitializeComponent();
        _timer.Tick += (_, _) => Actualizar();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _tieneBase = false;
        Actualizar();      // primera lectura (establece la base de CPU)
        _timer.Start();
        _ = MostrarModo();
        _ = Task.Run(() => _vivo ??= new MonitorVivo());
    }

    private async Task MostrarModo()
    {
        if (!Aether.Disponible)
        {
            TxtModo.Text = "Modo: —";
            return;
        }
        string modo = await Aether.ModoActual();
        TxtModo.Text = $"Modo {Ui.NombreModo(modo)}";
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _timer.Stop();
    }

    private void Actualizar()
    {
        ActualizarCpu();
        ActualizarRam();
        ActualizarUptime();

        // Los procesos y los contadores detallados cada 2 segundos (más costoso).
        if (_tick % 2 == 0)
        {
            ActualizarProcesos();
            _ = ActualizarDetalle();
        }
        _tick++;
    }

    private void ActualizarCpu()
    {
        if (!GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user))
        {
            return;
        }

        ulong i = ToU(idle), k = ToU(kernel), u = ToU(user);

        if (!_tieneBase)
        {
            _prevIdle = i; _prevKernel = k; _prevUser = u;
            _tieneBase = true;
            TxtCpu.Text = "…";
            return;
        }

        ulong totalDelta = (k - _prevKernel) + (u - _prevUser);  // kernel ya incluye idle
        ulong idleDelta = i - _prevIdle;
        _prevIdle = i; _prevKernel = k; _prevUser = u;

        double uso = totalDelta == 0 ? 0 : (1.0 - ((double)idleDelta / totalDelta)) * 100.0;
        uso = Math.Clamp(uso, 0, 100);

        BarraCpu.Value = uso;
        TxtCpu.Text = $"{uso:0}%";
        TxtCpuDetalle.Text = $"{Environment.ProcessorCount} núcleos lógicos";
    }

    private void ActualizarRam()
    {
        var m = new MemStatus();
        if (!GlobalMemoryStatusEx(m))
        {
            return;
        }

        double totalGb = m.TotalPhys / 1073741824.0;
        double dispGb = m.AvailPhys / 1073741824.0;
        double usadaGb = totalGb - dispGb;

        BarraRam.Value = m.MemoryLoad;
        TxtRam.Text = $"{m.MemoryLoad}%";
        TxtRamDetalle.Text = $"{usadaGb:0.0} GB usados de {totalGb:0.0} GB";
    }

    private async Task ActualizarDetalle()
    {
        if (_vivo is null || _muestreando)
        {
            return;
        }
        _muestreando = true;
        try
        {
            MuestraMonitor m = await Task.Run(() => _vivo.Muestrear());
            Detalle.Poner("Gráfica", double.IsNaN(m.GpuPct) ? "—" : $"{m.GpuPct:0}%");
            Detalle.Poner("Disco activo", double.IsNaN(m.DiscoPct) ? "—" : $"{m.DiscoPct:0}%");
            Detalle.Poner("Disco lee / escribe", $"{m.LecturaMBs:0.0} / {m.EscrituraMBs:0.0} MB/s");
            Detalle.Poner("Red baja / sube", $"{Kb(m.BajadaKBs)} / {Kb(m.SubidaKBs)}");
            Detalle.Poner("Temperatura", double.IsNaN(m.TempC) ? "no la informa" : $"{m.TempC:0} °C");
            Detalle.Poner("Procesador ahora", double.IsNaN(m.FrecuenciaGhz) ? "—" : $"{m.FrecuenciaGhz:0.00} GHz");
            if (!string.IsNullOrEmpty(m.GpuNombre))
            {
                Detalle.Poner("Tarjeta gráfica", m.GpuNombre);
            }

            PanelTop.Children.Clear();
            foreach (var p in m.Top)
            {
                var fila = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
                var nombre = new TextBlock { Text = p.Nombre, FontWeight = FontWeights.SemiBold };
                var cpu = new TextBlock { Text = $"{p.Cpu:0.0}% CPU", HorizontalAlignment = HorizontalAlignment.Right };
                var ram = new TextBlock { Text = $"{p.RamMb:0} MB", HorizontalAlignment = HorizontalAlignment.Right };
                Grid.SetColumn(cpu, 1);
                Grid.SetColumn(ram, 2);
                fila.Children.Add(nombre);
                fila.Children.Add(cpu);
                fila.Children.Add(ram);
                PanelTop.Children.Add(fila);
            }
        }
        catch { /* una muestra fallida no detiene el Monitor */ }
        finally
        {
            _muestreando = false;
        }
    }

    private static string Kb(double kbs) => kbs >= 1024 ? $"{kbs / 1024:0.0} MB/s" : $"{kbs:0} KB/s";

    private void ActualizarUptime()
    {
        TimeSpan t = TimeSpan.FromMilliseconds(GetTickCount64());
        TxtUptime.Text = t.TotalDays >= 1
            ? $"{(int)t.TotalDays} d {t.Hours} h {t.Minutes} min"
            : $"{t.Hours} h {t.Minutes} min";
    }

    private void ActualizarProcesos()
    {
        try
        {
            Process[] procs = Process.GetProcesses();
            TxtProcesos.Text = procs.Length.ToString();
            foreach (Process p in procs)
            {
                p.Dispose();   // liberar handles para no fugar en una página en vivo
            }
        }
        catch
        {
            _ = 0; // sin permisos para enumerar: dejamos el último valor
        }
    }

    // ── P/Invoke ─────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint Low;
        public uint High;
    }

    private static ulong ToU(FileTime ft) => ((ulong)ft.High << 32) | ft.Low;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);

    [StructLayout(LayoutKind.Sequential)]
    private sealed class MemStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;

        public MemStatus()
        {
            Length = (uint)Marshal.SizeOf(typeof(MemStatus));
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemStatus buffer);

    [DllImport("kernel32.dll")]
    private static extern ulong GetTickCount64();
}
