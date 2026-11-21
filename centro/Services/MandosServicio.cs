using System.Collections.Generic;
using Windows.Gaming.Input;

namespace CentroAether.Services;

/// <summary>Un mando detectado.</summary>
public sealed class MandoInfo
{
    public string Nombre { get; init; } = "Mando";
    public bool EsGamepad { get; init; }
    public string Marca { get; init; } = "Genérico";
    public string Protocolo { get; init; } = "HID";
    public ushort VendorId { get; init; }
}

/// <summary>
/// Detecta mandos conectados usando Windows.Gaming.Input (nativo). Base del
/// Aether Controller; el mapeo avanzado (emulación Xbox 360, perfiles) llegará
/// con el motor basado en DS4Windows.
/// </summary>
public static class MandosServicio
{
    public static List<MandoInfo> Detectar()
    {
        var lista = new List<MandoInfo>();
        try
        {
            foreach (RawGameController c in RawGameController.RawGameControllers)
            {
                string nombre = string.IsNullOrWhiteSpace(c.DisplayName) ? "Mando" : c.DisplayName;
                ushort vid = c.HardwareVendorId;
                string marca = vid switch
                {
                    0x054C => "PlayStation",
                    0x045E => "Xbox",
                    0x057E => "Nintendo",
                    _ => "Genérico",
                };

                bool gamepad = false;
                try
                {
                    gamepad = Gamepad.FromGameController(c) is not null;
                }
                catch { /* no es gamepad estándar */ }

                lista.Add(new MandoInfo
                {
                    Nombre = nombre,
                    EsGamepad = gamepad,
                    Marca = marca,
                    Protocolo = gamepad ? "XInput" : "HID / DirectInput",
                    VendorId = vid,
                });
            }
        }
        catch { /* sin acceso a la API de mandos */ }
        return lista;
    }
}
