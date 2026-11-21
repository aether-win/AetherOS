using System;
using System.Linq;
using HidSharp;

namespace CentroAether.Services;

/// <summary>
/// Lee un DualShock 4 por HID crudo con los offsets conocidos del informe de
/// entrada (USB report 0x01 y Bluetooth report 0x11, que desplaza +2). Esto es
/// fiable, a diferencia de adivinar índices con RawGameController. Produce el
/// estado neutral que el motor convierte a Xbox 360. (Técnica de DS4Windows, GPLv3.)
/// </summary>
public sealed class LectorDs4Hid : IDisposable
{
    private const int VendorSony = 0x054C;
    private static readonly int[] ProductosDs4 = { 0x05C4, 0x09CC, 0x0BA0 };

    private HidStream? _stream;
    private byte[] _buffer = Array.Empty<byte>();

    public bool Abierto => _stream is not null;

    public bool Abrir()
    {
        try
        {
            HidDevice? dev = DeviceList.Local
                .GetHidDevices(VendorSony)
                .FirstOrDefault(d => ProductosDs4.Contains(d.ProductID));

            if (dev is null)
            {
                return false;
            }

            if (dev.TryOpen(out HidStream stream))
            {
                stream.ReadTimeout = 40;
                _stream = stream;
                _buffer = new byte[Math.Max(dev.GetMaxInputReportLength(), 16)];
                return true;
            }
        }
        catch { /* sin acceso / ocupado */ }
        return false;
    }

    /// <summary>Lee un informe y devuelve el estado, o null si no hubo lectura.</summary>
    public EstadoMando? Leer()
    {
        if (_stream is null)
        {
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
        catch
        {
            return null;   // timeout o desconexión puntual
        }
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
        _stream?.Dispose();
        _stream = null;
    }

    public void Dispose() => Cerrar();
}
