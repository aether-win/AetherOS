using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using HidSharp;
using Microsoft.Win32.SafeHandles;

namespace CentroAether.Services;

/// <summary>
/// Lee un DualShock 4 REAL por HID crudo con los offsets conocidos del informe de entrada
/// (USB report 0x01 y Bluetooth report 0x11, que desplaza +2) y le manda el color de la barra
/// de luz como lo hace una PS4 (informe de salida 0x05 por USB, 0x11 con CRC32 por Bluetooth).
/// Nunca abre un mando virtual de ViGEm (sus rutas se pasan en "excluir"). Técnica de
/// DS4Windows / controlador hid-sony de Linux (GPLv2/GPLv3).
/// </summary>
public sealed class LectorDs4Hid : IDisposable
{
    public const int VendorSony = 0x054C;
    private static readonly int[] ProductosDs4 = { 0x05C4, 0x09CC, 0x0BA0 };

    private HidDevice? _dev;
    private HidStream? _stream;
    private byte[] _buffer = Array.Empty<byte>();

    public bool Abierto => _stream is not null;

    /// <summary>Ruta HID del mando abierto (\\?\hid#vid_054c&amp;pid_...).</summary>
    public string? Ruta => _dev?.DevicePath;

    public bool EsBluetooth => EsRutaBluetooth(Ruta);

    public static bool EsRutaBluetooth(string? ruta) =>
        ruta?.Contains("{00001124-", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>DualShock 4 conectados (reales y virtuales; usar "excluir" para quitar los virtuales).</summary>
    public static List<HidDevice> Buscar(ICollection<string>? excluir = null)
    {
        try
        {
            return DeviceList.Local.GetHidDevices(VendorSony)
                .Where(d => ProductosDs4.Contains(d.ProductID))
                .Where(d => excluir is null || !excluir.Contains(d.DevicePath, StringComparer.OrdinalIgnoreCase))
                .Where(d => SafeMaxEntrada(d) > 0)
                .ToList();
        }
        catch
        {
            return new List<HidDevice>();
        }
    }

    private static int SafeMaxEntrada(HidDevice d)
    {
        try { return d.GetMaxInputReportLength(); } catch { return 0; }
    }

    /// <summary>Abre un DualShock 4 real; si se indica, prefiere el de la ruta "preferida".</summary>
    public bool Abrir(ICollection<string>? excluir = null, string? preferida = null)
    {
        Cerrar();
        List<HidDevice> lista = Buscar(excluir);
        HidDevice? dev = lista.FirstOrDefault(d => string.Equals(d.DevicePath, preferida, StringComparison.OrdinalIgnoreCase))
                         ?? lista.FirstOrDefault();
        if (dev is null)
        {
            return false;
        }
        try
        {
            if (dev.TryOpen(out HidStream stream))
            {
                stream.ReadTimeout = 40;
                _stream = stream;
                _dev = dev;
                _buffer = new byte[Math.Max(dev.GetMaxInputReportLength(), 16)];
                return true;
            }
        }
        catch { /* sin acceso / ocupado */ }
        return false;
    }

    /// <summary>
    /// Lee un informe. Devuelve null si no hubo datos; "desconectado" indica que el mando se
    /// fue (hay que cerrarlo y volver a buscarlo), a diferencia de un simple tiempo agotado.
    /// </summary>
    public EstadoMando? Leer(out bool desconectado)
    {
        desconectado = false;
        if (_stream is null)
        {
            desconectado = true;
            return null;
        }
        try
        {
            int n = _stream.Read(_buffer);
            if (n < 10)
            {
                return null;
            }
            return Parsear(_buffer);
        }
        catch (TimeoutException)
        {
            return null;
        }
        catch
        {
            desconectado = true;
            return null;
        }
    }

    /// <summary>Pone la barra de luz del mando abierto en ese color.</summary>
    public bool EnviarLuz(byte r, byte g, byte b)
    {
        if (_dev is null)
        {
            return false;
        }
        int largo = 0;
        try { largo = _dev.GetMaxOutputReportLength(); } catch { /* usa el estándar */ }
        return LuzDs4.Enviar(_dev.DevicePath, EsBluetooth, largo, r, g, b);
    }

    private static EstadoMando Parsear(byte[] b)
    {
        // Offset +2 en Bluetooth (report id 0x11) respecto a USB (0x01).
        int o = b[0] == 0x11 ? 2 : 0;

        byte lx = b[1 + o], ly = b[2 + o], rx = b[3 + o], ry = b[4 + o];
        byte b5 = b[5 + o], b6 = b[6 + o], b7 = b[7 + o];
        byte l2 = b[8 + o], r2 = b[9 + o];

        int hat = b5 & 0x0F;   // 0..7 dirección, 8 = soltado

        return new EstadoMando
        {
            X = (b5 & 0x10) != 0,   // Square  -> X
            A = (b5 & 0x20) != 0,   // Cross   -> A
            B = (b5 & 0x40) != 0,   // Circle  -> B
            Y = (b5 & 0x80) != 0,   // Triangle-> Y
            L1 = (b6 & 0x01) != 0,
            R1 = (b6 & 0x02) != 0,
            Atras = (b6 & 0x10) != 0,   // Share   -> Back
            Inicio = (b6 & 0x20) != 0,  // Options -> Start
            L3 = (b6 & 0x40) != 0,
            R3 = (b6 & 0x80) != 0,
            Guia = (b7 & 0x01) != 0,    // PS      -> Guide
            Up = hat is 7 or 0 or 1,
            Right = hat is 1 or 2 or 3,
            Down = hat is 3 or 4 or 5,
            Left = hat is 5 or 6 or 7,
            LX = Centro(lx),
            LY = -Centro(ly),   // byte Y: 0 arriba, 255 abajo -> invertir
            RX = Centro(rx),
            RY = -Centro(ry),
            LT = l2 / 255.0,
            RT = r2 / 255.0,
        };
    }

    private static double Centro(byte v) => Math.Clamp((v - 128.0) / 127.0, -1.0, 1.0);

    public void Cerrar()
    {
        try { _stream?.Close(); } catch { /* ignore */ }
        try { _stream?.Dispose(); } catch { /* ignore */ }
        _stream = null;
        _dev = null;
    }

    public void Dispose() => Cerrar();
}

/// <summary>
/// Barra de luz del DualShock 4 (como en una PS4). El mando original de Sony no enciende la
/// luz hasta que el equipo se lo pide; los genéricos sí, por eso solo fallaba el original.
/// </summary>
public static class LuzDs4
{
    /// <summary>Azul del jugador 1 de PS4.</summary>
    public static readonly (byte R, byte G, byte B) Jugador1 = (0x00, 0x18, 0xD8);

    private const byte FlagLuz = 0x02;      // cambiar color de la barra
    private const byte FlagParpadeo = 0x04; // y quitar parpadeo (luz fija)

    public static bool Enviar(string ruta, bool bluetooth, int largoSalida, byte r, byte g, byte b)
    {
        using SafeFileHandle h = CreateFileW(ruta, 0xC0000000, 0x00000003, IntPtr.Zero, 3, 0, IntPtr.Zero);
        if (h.IsInvalid)
        {
            return false;
        }

        if (bluetooth)
        {
            // Informe 0x11 de 78 bytes: [1]=HID+CRC, [3]=qué cambia, [8..10]=RGB, [74..77]=CRC32.
            var buf = new byte[78];
            buf[0] = 0x11;
            buf[1] = 0xC0;
            buf[3] = FlagLuz | FlagParpadeo;
            buf[8] = r;
            buf[9] = g;
            buf[10] = b;
            uint crc = Crc32(new byte[] { 0xA2 }.Concat(buf.Take(74)).ToArray());
            BitConverter.GetBytes(crc).CopyTo(buf, 74);

            if (HidD_SetOutputReport(h, buf, buf.Length))
            {
                return true;
            }
            // Respaldo: escribirlo por la vía normal con el largo que pide Windows.
            byte[] largo = new byte[Math.Max(buf.Length, largoSalida)];
            buf.CopyTo(largo, 0);
            return WriteFile(h, largo, largo.Length, out _, IntPtr.Zero);
        }
        else
        {
            // Informe 0x05 por USB: [1]=qué cambia, [6..8]=RGB.
            var buf = new byte[Math.Max(32, largoSalida)];
            buf[0] = 0x05;
            buf[1] = FlagLuz | FlagParpadeo;
            buf[6] = r;
            buf[7] = g;
            buf[8] = b;
            return WriteFile(h, buf, buf.Length, out _, IntPtr.Zero)
                   || HidD_SetOutputReport(h, buf, buf.Length);
        }
    }

    // ── Mandos conectados sin emulación: encender su luz una vez al conectarse ─────────

    private static readonly HashSet<string> Encendidos = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Enciende la luz (azul jugador 1) de cada DualShock 4 recién conectado.</summary>
    public static void AsegurarEnConectados()
    {
        List<HidDevice> mandos = LectorDs4Hid.Buscar();
        var presentes = new HashSet<string>(mandos.Select(m => m.DevicePath), StringComparer.OrdinalIgnoreCase);
        Encendidos.RemoveWhere(r => !presentes.Contains(r));
        foreach (HidDevice m in mandos)
        {
            if (Encendidos.Contains(m.DevicePath))
            {
                continue;
            }
            int largo = 0;
            try { largo = m.GetMaxOutputReportLength(); } catch { /* estándar */ }
            if (Enviar(m.DevicePath, LectorDs4Hid.EsRutaBluetooth(m.DevicePath), largo, Jugador1.R, Jugador1.G, Jugador1.B))
            {
                Encendidos.Add(m.DevicePath);
            }
        }
    }

    // ── CRC32 (el que exige el DualShock 4 por Bluetooth) ──────────────────────────────

    private static readonly uint[] Tabla = CrearTabla();

    private static uint[] CrearTabla()
    {
        var t = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }
            t[i] = c;
        }
        return t;
    }

    private static uint Crc32(byte[] datos)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (byte d in datos)
        {
            crc = Tabla[(crc ^ d) & 0xFF] ^ (crc >> 8);
        }
        return ~crc;
    }

    // ── Win32 ────────────────────────────────────────────────────────────────────────

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string nombre, uint acceso, uint compartir, IntPtr seguridad,
        uint creacion, uint atributos, IntPtr plantilla);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(SafeFileHandle h, byte[] datos, int largo, out int escritos, IntPtr overlapped);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_SetOutputReport(SafeFileHandle h, byte[] informe, int largo);
}
