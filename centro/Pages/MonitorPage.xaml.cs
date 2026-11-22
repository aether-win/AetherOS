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

        // Los procesos se cuentan cada 2 segundos (más costoso).
        if (_tick % 2 == 0)
        {
            ActualizarProcesos();
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
