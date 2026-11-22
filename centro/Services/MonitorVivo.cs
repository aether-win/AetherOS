using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;

namespace CentroAether.Services;

/// <summary>Una muestra del Monitor en vivo.</summary>
public sealed record MuestraMonitor(
    double GpuPct,
    string GpuNombre,
    double DiscoPct,
    double LecturaMBs,
    double EscrituraMBs,
    double BajadaKBs,
    double SubidaKBs,
    double TempC,          // NaN = el equipo no la informa
    double FrecuenciaGhz,  // NaN = desconocida
    IReadOnlyList<(string Nombre, double Cpu, double RamMb)> Top);

/// <summary>
/// Contadores en vivo para el Monitor: GPU (motores 3D, como el Administrador de tareas), disco,
/// red, temperatura, frecuencia real de la CPU y los procesos que más consumen. Aether Toolbox
/// corre como administrador, que es lo que Windows exige para leer todos estos datos.
/// </summary>
public sealed class MonitorVivo : IDisposable
{
    private readonly Dictionary<string, PerformanceCounter> _gpu = new();
    private DateTime _gpuInstancias = DateTime.MinValue;
    private PerformanceCounter? _discoInactivo, _lectura, _escritura, _rendimiento;
    private readonly Dictionary<int, TimeSpan> _cpuPrevio = new();
    private DateTime _muestraPrevia = DateTime.MinValue;
    private long _rxPrevio, _txPrevio;
    private readonly double _maxGhz = double.NaN;
    private readonly string _gpuNombre = "";

    public MonitorVivo()
    {
        try { _discoInactivo = new PerformanceCounter("PhysicalDisk", "% Idle Time", "_Total"); _discoInactivo.NextValue(); } catch { _discoInactivo = null; }
        try { _lectura = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total"); _lectura.NextValue(); } catch { _lectura = null; }
        try { _escritura = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total"); _escritura.NextValue(); } catch { _escritura = null; }
        try { _rendimiento = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total"); _rendimiento.NextValue(); } catch { _rendimiento = null; }
        try
        {
            using var s = new ManagementObjectSearcher("SELECT MaxClockSpeed FROM Win32_Processor");
            foreach (ManagementObject o in s.Get()) { _maxGhz = Convert.ToDouble(o["MaxClockSpeed"]) / 1000.0; break; }
        }
        catch { /* sin WMI */ }
        try
        {
            using var s = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            _gpuNombre = string.Join(" + ", s.Get().Cast<ManagementObject>().Select(o => (o["Name"] as string ?? "").Trim()).Where(n => n.Length > 0));
        }
        catch { /* sin WMI */ }
        (_rxPrevio, _txPrevio) = BytesRed();
    }

    public MuestraMonitor Muestrear()
    {
        DateTime ahora = DateTime.UtcNow;
        double segundos = _muestraPrevia == DateTime.MinValue ? 1 : Math.Max(0.2, (ahora - _muestraPrevia).TotalSeconds);
        _muestraPrevia = ahora;

        double gpu = LeerGpu();
        double disco = _discoInactivo is null ? double.NaN : Math.Clamp(100 - _discoInactivo.NextValue(), 0, 100);
        double lect = _lectura is null ? 0 : _lectura.NextValue() / 1048576.0;
        double escr = _escritura is null ? 0 : _escritura.NextValue() / 1048576.0;

        (long rx, long tx) = BytesRed();
        double bajada = Math.Max(0, rx - _rxPrevio) / 1024.0 / segundos;
        double subida = Math.Max(0, tx - _txPrevio) / 1024.0 / segundos;
        _rxPrevio = rx; _txPrevio = tx;

        double frec = _rendimiento is null || double.IsNaN(_maxGhz) ? double.NaN : _maxGhz * _rendimiento.NextValue() / 100.0;
        return new MuestraMonitor(gpu, _gpuNombre, disco, lect, escr, bajada, subida, LeerTemperatura(), frec, TopProcesos(segundos));
    }

    private double LeerGpu()
    {
        try
        {
            if ((DateTime.UtcNow - _gpuInstancias).TotalSeconds > 10)
            {
                _gpuInstancias = DateTime.UtcNow;
                var cat = new PerformanceCounterCategory("GPU Engine");
                var actuales = cat.GetInstanceNames().Where(n => n.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase)).ToHashSet();
                foreach (string viejo in _gpu.Keys.Where(k => !actuales.Contains(k)).ToList())
                {
                    _gpu[viejo].Dispose();
                    _gpu.Remove(viejo);
                }
                foreach (string nuevo in actuales.Where(n => !_gpu.ContainsKey(n)))
                {
                    var c = new PerformanceCounter("GPU Engine", "Utilization Percentage", nuevo, true);
                    c.NextValue();
                    _gpu[nuevo] = c;
                }
            }
            double total = 0;
            foreach (var c in _gpu.Values)
            {
                try { total += c.NextValue(); } catch { /* el proceso terminó */ }
            }
            return Math.Clamp(total, 0, 100);
        }
        catch
        {
            return double.NaN;
        }
    }

    private static double LeerTemperatura()
    {
        try
        {
            using var s = new ManagementObjectSearcher(@"root\wmi", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            double max = double.NaN;
            foreach (ManagementObject o in s.Get())
            {
                double c = Convert.ToDouble(o["CurrentTemperature"]) / 10.0 - 273.15;
                if (c > 0 && c < 125 && (double.IsNaN(max) || c > max)) { max = c; }
            }
            return max;
        }
        catch
        {
            return double.NaN;
        }
    }

    private static (long Rx, long Tx) BytesRed()
    {
        long rx = 0, tx = 0;
        try
        {
            foreach (NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (n.OperationalStatus != OperationalStatus.Up || n.NetworkInterfaceType == NetworkInterfaceType.Loopback) { continue; }
                IPInterfaceStatistics e = n.GetIPStatistics();
                rx += e.BytesReceived;
                tx += e.BytesSent;
            }
        }
        catch { /* sin acceso */ }
        return (rx, tx);
    }

    private IReadOnlyList<(string Nombre, double Cpu, double RamMb)> TopProcesos(double segundos)
    {
        var lista = new List<(string, double, double)>();
        var vistos = new HashSet<int>();
        int nucleos = Environment.ProcessorCount;
        foreach (Process p in Process.GetProcesses())
        {
            using (p)
            {
                try
                {
                    if (p.Id == 0) { continue; }
                    TimeSpan t = p.TotalProcessorTime;
                    vistos.Add(p.Id);
                    double cpu = 0;
                    if (_cpuPrevio.TryGetValue(p.Id, out TimeSpan antes))
                    {
                        cpu = (t - antes).TotalSeconds / segundos / nucleos * 100.0;
                    }
                    _cpuPrevio[p.Id] = t;
                    lista.Add((p.ProcessName, Math.Clamp(cpu, 0, 100), p.WorkingSet64 / 1048576.0));
                }
                catch { /* proceso protegido o terminado */ }
            }
        }
        foreach (int id in _cpuPrevio.Keys.Where(k => !vistos.Contains(k)).ToList()) { _cpuPrevio.Remove(id); }
        return lista.OrderByDescending(x => x.Item2).ThenByDescending(x => x.Item3).Take(6).ToList();
    }

    public void Dispose()
    {
        foreach (var c in _gpu.Values) { c.Dispose(); }
        _discoInactivo?.Dispose(); _lectura?.Dispose(); _escritura?.Dispose(); _rendimiento?.Dispose();
    }
}
