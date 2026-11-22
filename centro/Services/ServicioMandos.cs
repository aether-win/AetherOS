using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HidSharp;
using Windows.Gaming.Input;

namespace CentroAether.Services;

public enum TipoMando { DualShock4, DualSense, Xbox, Otro }

public enum ConexionMando { Usb, Bluetooth, Inalambrico, Desconocida }

/// <summary>Un mando físico conectado (nunca uno virtual de Aether).</summary>
public sealed class MandoConectado
{
    public string Id { get; init; } = "";
    public string Nombre { get; init; } = "Mando";
    public TipoMando Tipo { get; init; }
    public ConexionMando Conexion { get; init; }
    public string? RutaHid { get; init; }
    public DateTime Desde { get; init; } = DateTime.Now;

    public string TextoConexion => Conexion switch
    {
        ConexionMando.Usb => "USB",
        ConexionMando.Bluetooth => "Bluetooth",
        ConexionMando.Inalambrico => "Inalámbrico",
        _ => "Conectado",
    };
}

/// <summary>
/// Servicio de mandos de Aether Toolbox (siempre en segundo plano mientras el interruptor general
/// esté encendido). Funciona por EVENTOS, no revisando a cada rato:
///  - Detecta conexión y desconexión física (cambios de dispositivos HID y de Windows.Gaming.Input).
///  - Identifica el tipo (DualShock 4, DualSense, Xbox, otro) y si es USB o Bluetooth.
///  - Nunca muestra los mandos virtuales de Aether (evita duplicados).
///  - Enciende la luz del DualShock 4 original al conectarlo (azul jugador 1).
///  - Apaga el mando Bluetooth tras X minutos sin uso (solo ese mando, no el Bluetooth del equipo;
///    misma técnica que DS4Windows: IOCTL_BTH_DISCONNECT_DEVICE).
///  - Registra todo en %ProgramData%\Aether\mandos.log.
/// </summary>
public static class ServicioMandos
{
    private static readonly object Candado = new();
    private static readonly string RutaLog = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Aether", "mandos.log");

    private static List<MandoConectado> _conectados = new();
    private static Timer? _rebote;
    private static bool _activo;
    private static readonly Dictionary<string, CancellationTokenSource> _vigiasInactividad = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Se dispara (en cualquier hilo) cuando cambia la lista de mandos conectados.</summary>
    public static event Action? Cambio;

    /// <summary>Mando que se acaba de conectar o desconectar (para el aviso de la bandeja).</summary>
    public static event Action<MandoConectado, bool>? Conexion;

    public static IReadOnlyList<MandoConectado> Conectados
    {
        get { lock (Candado) { return _conectados.ToList(); } }
    }

    public static bool Activo => _activo;

    public static void Iniciar()
    {
        if (_activo || !ConfigCentro.MandosActivos)
        {
            return;
        }
        _activo = true;
        _rebote = new Timer(_ => Refrescar(), null, Timeout.Infinite, Timeout.Infinite);
        try { DeviceList.Local.Changed += AlCambiarDispositivos; } catch { /* sin HID */ }
        try
        {
            RawGameController.RawGameControllerAdded += AlCambiarMandoWindows;
            RawGameController.RawGameControllerRemoved += AlCambiarMandoWindows;
        }
        catch { /* sin Windows.Gaming.Input */ }
        MotorControlador.EstadoCambiado += Programar;
        Registrar("Servicio de mandos iniciado.");
        Programar();
    }

    public static void Detener()
    {
        if (!_activo)
        {
            return;
        }
        _activo = false;
        try { DeviceList.Local.Changed -= AlCambiarDispositivos; } catch { /* ignore */ }
        try
        {
            RawGameController.RawGameControllerAdded -= AlCambiarMandoWindows;
            RawGameController.RawGameControllerRemoved -= AlCambiarMandoWindows;
        }
        catch { /* ignore */ }
        MotorControlador.EstadoCambiado -= Programar;
        _rebote?.Dispose();
        _rebote = null;
        lock (Candado)
        {
            foreach (CancellationTokenSource c in _vigiasInactividad.Values)
            {
                c.Cancel();
            }
            _vigiasInactividad.Clear();
            _conectados = new List<MandoConectado>();
        }
        Registrar("Servicio de mandos detenido (interruptor general apagado).");
        Cambio?.Invoke();
    }

    private static void AlCambiarDispositivos(object? s, DeviceListChangedEventArgs e) => Programar();

    private static void AlCambiarMandoWindows(object? s, RawGameController e) => Programar();

    /// <summary>Agrupa ráfagas de eventos (Windows avisa varias veces por un mismo mando).</summary>
    public static void Programar()
    {
        try { _rebote?.Change(600, Timeout.Infinite); } catch { /* cerrando */ }
    }

    private static void Refrescar()
    {
        if (!_activo)
        {
            return;
        }
        var nuevos = new List<MandoConectado>();

        // 1) Mandos Sony por HID (DualShock 4 y DualSense), sin los virtuales de Aether.
        try
        {
            foreach (HidDevice d in DeviceList.Local.GetHidDevices(LectorDs4Hid.VendorSony))
            {
                bool ds4 = LectorDs4Hid.EsDs4(d.ProductID);
                bool dualSense = Array.IndexOf(LectorDs4Hid.ProductosDualSense, d.ProductID) >= 0;
                if ((!ds4 && !dualSense) || MotorControlador.EsRutaVirtual(d.DevicePath))
                {
                    continue;
                }
                int entrada = 0;
                try { entrada = d.GetMaxInputReportLength(); } catch { /* sin datos */ }
                if (entrada <= 0 || nuevos.Any(n => string.Equals(n.RutaHid, d.DevicePath, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                nuevos.Add(new MandoConectado
                {
                    Id = d.DevicePath,
                    RutaHid = d.DevicePath,
                    Tipo = ds4 ? TipoMando.DualShock4 : TipoMando.DualSense,
                    Nombre = ds4 ? "DualShock 4 (PS4)" : "DualSense (PS5)",
                    Conexion = LectorDs4Hid.EsRutaBluetooth(d.DevicePath) ? ConexionMando.Bluetooth : ConexionMando.Usb,
                });
            }
        }
        catch (Exception ex) { Registrar("Error al buscar mandos HID: " + ex.Message); }

        // 2) Otros mandos (Xbox, XInput, genéricos) por Windows.Gaming.Input, sin el virtual.
        try
        {
            foreach (RawGameController rc in RawGameController.RawGameControllers)
            {
                string id = rc.NonRoamableId ?? "";
                if (rc.HardwareVendorId == LectorDs4Hid.VendorSony || MotorControlador.EsIdVirtual(id))
                {
                    continue;   // los Sony ya se cuentan por HID (más fiable)
                }
                bool xbox = rc.HardwareVendorId == 0x045E;
                bool esGamepad = false;
                try { esGamepad = Gamepad.FromGameController(rc) is not null; } catch { /* no estándar */ }
                if (!xbox && !esGamepad)
                {
                    continue;   // volantes, sticks de vuelo, etc. no cuentan como "mando"
                }
                nuevos.Add(new MandoConectado
                {
                    Id = id.Length > 0 ? id : rc.DisplayName,
                    Nombre = string.IsNullOrWhiteSpace(rc.DisplayName) ? (xbox ? "Mando Xbox" : "Mando") : rc.DisplayName,
                    Tipo = xbox ? TipoMando.Xbox : TipoMando.Otro,
                    Conexion = rc.IsWireless ? ConexionMando.Inalambrico : ConexionMando.Usb,
                });
            }
        }
        catch (Exception ex) { Registrar("Error al buscar mandos de Windows: " + ex.Message); }

        List<MandoConectado> antes;
        lock (Candado)
        {
            antes = _conectados;
            // Conservar la hora de conexión de los que siguen.
            nuevos = nuevos.Select(n => antes.FirstOrDefault(a => a.Id == n.Id) ?? n).ToList();
            _conectados = nuevos;
        }

        var llegaron = nuevos.Where(n => antes.All(a => a.Id != n.Id)).ToList();
        var salieron = antes.Where(a => nuevos.All(n => n.Id != a.Id)).ToList();
        if (llegaron.Count == 0 && salieron.Count == 0)
        {
            return;
        }

        foreach (MandoConectado m in salieron)
        {
            Registrar($"Desconectado: {m.Nombre} ({m.TextoConexion}).");
            DetenerVigia(m.Id);
            Conexion?.Invoke(m, false);
        }
        foreach (MandoConectado m in llegaron)
        {
            Registrar($"Conectado: {m.Nombre} ({m.TextoConexion}). Ruta: {m.RutaHid ?? m.Id}");
            if (m.Tipo == TipoMando.DualShock4 && m.RutaHid is not null)
            {
                EncenderLuz(m);
            }
            if (m.Conexion == ConexionMando.Bluetooth && m.RutaHid is not null)
            {
                IniciarVigia(m);
            }
            Conexion?.Invoke(m, true);
        }
        Cambio?.Invoke();
    }

    // ── Luz del DualShock 4 ──────────────────────────────────────────────────────────

    private static void EncenderLuz(MandoConectado m)
    {
        _ = Task.Run(async () =>
        {
            // Dos intentos: al conectar y a los 3 s (Steam u otra app pueden pisarla al arrancar).
            for (int i = 0; i < 2; i++)
            {
                int largo = 0;
                try
                {
                    HidDevice? d = DeviceList.Local.GetHidDevices(LectorDs4Hid.VendorSony)
                        .FirstOrDefault(x => string.Equals(x.DevicePath, m.RutaHid, StringComparison.OrdinalIgnoreCase));
                    largo = d?.GetMaxOutputReportLength() ?? 0;
                }
                catch { /* largo estándar */ }
                bool ok = LuzDs4.Enviar(m.RutaHid!, m.Conexion == ConexionMando.Bluetooth, largo,
                    LuzDs4.Jugador1.R, LuzDs4.Jugador1.G, LuzDs4.Jugador1.B);
                Registrar($"Luz {(i == 0 ? "al conectar" : "a los 3 s")}: {(ok ? "OK" : "FALLÓ")} — {LuzDs4.UltimoDetalle} (largo de salida de Windows: {largo}).");
                await Task.Delay(3000);
            }
        });
    }

    // ── Apagado por inactividad (solo Bluetooth) ─────────────────────────────────────

    /// <summary>Reinicia los vigías (por ejemplo, al cambiar el tiempo en la configuración).</summary>
    public static void AplicarTemporizador()
    {
        foreach (MandoConectado m in Conectados.Where(c => c.Conexion == ConexionMando.Bluetooth && c.RutaHid is not null))
        {
            DetenerVigia(m.Id);
            IniciarVigia(m);
        }
    }

    private static void IniciarVigia(MandoConectado m)
    {
        if (ConfigCentro.ApagadoMandoMin <= 0)
        {
            return;
        }
        var cts = new CancellationTokenSource();
        lock (Candado)
        {
            if (_vigiasInactividad.ContainsKey(m.Id))
            {
                return;
            }
            _vigiasInactividad[m.Id] = cts;
        }
        var hilo = new Thread(() => VigilarInactividad(m, cts.Token)) { IsBackground = true, Name = "Aether-inactividad" };
        hilo.Start();
    }

    private static void DetenerVigia(string id)
    {
        lock (Candado)
        {
            if (_vigiasInactividad.Remove(id, out CancellationTokenSource? c))
            {
                c.Cancel();
            }
        }
    }

    private static void VigilarInactividad(MandoConectado m, CancellationToken ct)
    {
        try
        {
            HidDevice? dev = DeviceList.Local.GetHidDevices(LectorDs4Hid.VendorSony)
                .FirstOrDefault(x => string.Equals(x.DevicePath, m.RutaHid, StringComparison.OrdinalIgnoreCase));
            if (dev is null || !dev.TryOpen(out HidStream stream))
            {
                Registrar($"Apagado automático: no se pudo abrir {m.Nombre} para medir el uso.");
                return;
            }
            using (stream)
            {
                stream.ReadTimeout = 1000;
                var buf = new byte[Math.Max(dev.GetMaxInputReportLength(), 16)];
                byte[]? anterior = null;
                DateTime ultimoUso = DateTime.UtcNow;
                while (!ct.IsCancellationRequested)
                {
                    int minutos = ConfigCentro.ApagadoMandoMin;
                    if (minutos <= 0)
                    {
                        return;
                    }
                    try
                    {
                        int n = stream.Read(buf);
                        if (n >= 10)
                        {
                            byte[] actual = Firma(buf);
                            if (anterior is null || HuboUso(anterior, actual))
                            {
                                ultimoUso = DateTime.UtcNow;
                            }
                            anterior = actual;
                        }
                    }
                    catch (TimeoutException) { /* sin informe: sigue contando */ }
                    catch { return; }   // se desconectó

                    if (DateTime.UtcNow - ultimoUso >= TimeSpan.FromMinutes(minutos))
                    {
                        string? mac = MacDe(dev);
                        bool ok = mac is not null && DesconectarBluetooth(mac);
                        Registrar($"Apagado automático tras {minutos} min sin uso: {m.Nombre} → {(ok ? "apagado" : "no se pudo (MAC " + (mac ?? "desconocida") + ")")}.");
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Registrar($"Apagado automático: error con {m.Nombre}: {ex.Message}");
        }
    }

    /// <summary>Botones y sticks del informe (sin giroscopio ni contadores, que cambian solos).</summary>
    private static byte[] Firma(byte[] b)
    {
        int o = b[0] == 0x11 ? 2 : 0;
        var f = new byte[9];
        Array.Copy(b, 1 + o, f, 0, 9);   // LX LY RX RY, botones (3 bytes), L2 R2
        f[6] &= 0x03;                     // byte 7: solo PS y touchpad (el resto es contador)
        return f;
    }

    private static bool HuboUso(byte[] a, byte[] b)
    {
        for (int i = 0; i < 4; i++)
        {
            if (Math.Abs(a[i] - b[i]) > 12)   // sticks: ignora el temblor normal
            {
                return true;
            }
        }
        for (int i = 4; i < 9; i++)
        {
            if (i >= 7 ? Math.Abs(a[i] - b[i]) > 12 : a[i] != b[i])
            {
                return true;
            }
        }
        return false;
    }

    private static string? MacDe(HidDevice d)
    {
        try
        {
            string serie = d.GetSerialNumber() ?? "";
            string hex = new(serie.Where(Uri.IsHexDigit).ToArray());
            return hex.Length == 12 ? hex : null;
        }
        catch
        {
            return null;
        }
    }

    // ── Desconectar UN dispositivo Bluetooth (no el adaptador) ────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct ParamsBuscarRadio
    {
        public int Tamano;
    }

    [DllImport("bthprops.cpl", SetLastError = true)]
    private static extern IntPtr BluetoothFindFirstRadio(ref ParamsBuscarRadio p, out IntPtr radio);

    [DllImport("bthprops.cpl", SetLastError = true)]
    private static extern bool BluetoothFindNextRadio(IntPtr busqueda, out IntPtr radio);

    [DllImport("bthprops.cpl", SetLastError = true)]
    private static extern bool BluetoothFindRadioClose(IntPtr busqueda);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(IntPtr h, uint codigo, ref long entrada, int tamEntrada,
        IntPtr salida, int tamSalida, out int devueltos, IntPtr overlapped);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr h);

    private const uint IoctlBthDisconnectDevice = 0x41000C;

    private static bool DesconectarBluetooth(string macHex)
    {
        long direccion = long.Parse(macHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var p = new ParamsBuscarRadio { Tamano = Marshal.SizeOf<ParamsBuscarRadio>() };
        IntPtr busqueda = BluetoothFindFirstRadio(ref p, out IntPtr radio);
        if (busqueda == IntPtr.Zero)
        {
            return false;
        }
        bool ok = false;
        try
        {
            do
            {
                ok = DeviceIoControl(radio, IoctlBthDisconnectDevice, ref direccion, sizeof(long), IntPtr.Zero, 0, out _, IntPtr.Zero);
                CloseHandle(radio);
                if (ok)
                {
                    break;
                }
            }
            while (BluetoothFindNextRadio(busqueda, out radio));
        }
        finally
        {
            BluetoothFindRadioClose(busqueda);
        }
        return ok;
    }

    // ── Registro ─────────────────────────────────────────────────────────────────────

    public static void Registrar(string texto)
    {
        try
        {
            lock (Candado)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RutaLog)!);
                var info = new FileInfo(RutaLog);
                if (info.Exists && info.Length > 512 * 1024)
                {
                    File.Move(RutaLog, RutaLog + ".anterior", overwrite: true);
                }
                File.AppendAllText(RutaLog, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {texto}{Environment.NewLine}");
            }
        }
        catch { /* el registro nunca debe fallar */ }
    }

    public static string CarpetaLog => Path.GetDirectoryName(RutaLog)!;
}
