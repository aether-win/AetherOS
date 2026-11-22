using System;
using System.Linq;
using System.Management;
using System.Windows.Forms;

namespace CentroAether.Services;

/// <summary>Lectura en vivo de la batería (lo mismo que el informe de powercfg, sin abrir el navegador).</summary>
public sealed record EstadoBateria(
    bool Hay,
    int Porcentaje,
    bool Conectado,
    bool Cargando,
    int TasaMw,            // + cargando, - descargando
    int VoltajeMv,
    int RestanteMwh,
    int CompletaMwh,
    int DisenoMwh,
    int Ciclos,
    int SaludPct,
    int MinutosRestantes,  // -1 = desconocido
    string Fabricante,
    string Modelo);

public static class BateriaVivo
{
    private static string _fabricante = "", _modelo = "";
    private static int _diseno = -1;

    /// <summary>Lee el estado actual. Requiere privilegios de administrador (Aether Toolbox los tiene).</summary>
    public static EstadoBateria Leer()
    {
        PowerStatus ps = SystemInformation.PowerStatus;
        bool hay = ps.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery && ps.BatteryChargeStatus != BatteryChargeStatus.Unknown;
        int pct = (int)Math.Round(ps.BatteryLifePercent * 100);
        bool conectado = ps.PowerLineStatus == PowerLineStatus.Online;
        int minutos = ps.BatteryLifeRemaining > 0 ? ps.BatteryLifeRemaining / 60 : -1;

        int tasa = 0, voltaje = 0, restante = 0, completa = 0, ciclos = -1;
        bool cargando = (ps.BatteryChargeStatus & BatteryChargeStatus.Charging) != 0;
        try
        {
            foreach (ManagementObject o in Consulta("SELECT * FROM BatteryStatus"))
            {
                hay = true;
                restante = AInt(o["RemainingCapacity"]);
                voltaje = AInt(o["Voltage"]);
                int carga = AInt(o["ChargeRate"]), descarga = AInt(o["DischargeRate"]);
                cargando = o["Charging"] is bool c ? c : cargando;
                conectado = o["PowerOnline"] is bool p ? p : conectado;
                tasa = cargando ? carga : -descarga;
                break;
            }
            foreach (ManagementObject o in Consulta("SELECT * FROM BatteryFullChargedCapacity"))
            {
                completa = AInt(o["FullChargedCapacity"]);
                break;
            }
            foreach (ManagementObject o in Consulta("SELECT * FROM BatteryCycleCount"))
            {
                ciclos = AInt(o["CycleCount"]);
                break;
            }
            if (_diseno < 0)
            {
                _diseno = 0;
                foreach (ManagementObject o in Consulta("SELECT * FROM BatteryStaticData"))
                {
                    _diseno = AInt(o["DesignedCapacity"]);
                    _fabricante = (o["ManufactureName"] as string ?? "").Trim();
                    _modelo = (o["DeviceName"] as string ?? "").Trim();
                    break;
                }
            }
        }
        catch { /* el driver no expone estas clases: se queda con lo básico */ }

        // Tiempo restante calculado con la tasa real si Windows no lo da (o hasta llenar al cargar).
        if (tasa < 0 && restante > 0)
        {
            minutos = (int)(restante / (double)-tasa * 60);
        }
        else if (tasa > 0 && completa > restante)
        {
            minutos = (int)((completa - restante) / (double)tasa * 60);
        }
        if (completa > 0 && restante > 0 && !hay) { hay = true; }

        int salud = _diseno > 0 && completa > 0 ? (int)Math.Round(completa * 100.0 / _diseno) : -1;
        return new EstadoBateria(hay, Math.Clamp(pct, 0, 100), conectado, cargando, tasa, voltaje, restante, completa,
            _diseno, ciclos, salud, minutos, _fabricante, _modelo);
    }

    private static ManagementObjectCollection Consulta(string wql)
    {
        using var s = new ManagementObjectSearcher(@"root\wmi", wql);
        return s.Get();
    }

    private static int AInt(object? v) => v is null ? 0 : Convert.ToInt32(v);

    public static string Tiempo(int minutos) =>
        minutos < 0 ? "calculando…" : minutos >= 60 ? $"{minutos / 60} h {minutos % 60} min" : $"{minutos} min";

    public static string Tasa(int mw) =>
        mw == 0 ? "0 W" : $"{(mw > 0 ? "+" : "−")}{Math.Abs(mw) / 1000.0:0.0} W";
}
