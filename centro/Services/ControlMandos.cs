using System;
using System.IO;
using System.Media;

namespace CentroAether.Services;

/// <summary>
/// Cambio de mando (Xbox 360 / PS4 / apagado) con COMPROBACIÓN y sonido de Windows. Lo usan
/// la bandeja, los perfiles y la sección Control, para que todos hagan lo mismo:
///  1. comprueba que los mandos de Aether estén activos y que haya driver;
///  2. aplica el cambio; 3. verifica que de verdad quedó así;
///  4. solo entonces suena (un sonido distinto para Xbox, PS4 y apagado), si los sonidos están activos.
/// </summary>
public static class ControlMandos
{
    public static (bool Ok, string Mensaje) Cambiar(TipoVirtual? destino)
    {
        if (!ConfigCentro.MandosActivos)
        {
            return (false, "Los mandos de Aether están desactivados (Aether Toolbox › Control).");
        }

        if (destino is null)
        {
            if (!MotorControlador.Activo)
            {
                return (true, "El mando ya estaba en modo normal.");
            }
            MotorControlador.Detener();
            bool ok = !MotorControlador.Activo;
            if (ok)
            {
                Sonar(null);
            }
            return (ok, ok ? "Mando normal (sin emulación)." : "No se pudo detener la emulación.");
        }

        if (MotorControlador.Activo && MotorControlador.Objetivo == destino)
        {
            return (true, $"El mando ya funciona como {Nombre(destino)}.");
        }
        if (ServicioMandos.Activo && ServicioMandos.Conectados.Count == 0)
        {
            return (false, "No hay ningún mando conectado.");
        }
        if (!MotorControlador.ViGEmDisponible())
        {
            return (false, "Falta el driver ViGEmBus. Instálalo en Aether Toolbox › Control.");
        }
        string? conflicto = MotorControlador.ConflictoActivo();
        if (conflicto is not null)
        {
            return (false, $"{conflicto} está abierto. Ciérralo para evitar entrada doble.");
        }

        if (MotorControlador.Activo)
        {
            MotorControlador.Detener();
        }
        MotorControlador.Objetivo = destino.Value;
        MotorControlador.Iniciar();

        bool verificado = MotorControlador.Activo && MotorControlador.Objetivo == destino;
        ServicioMandos.Registrar($"Cambio de mando a {Nombre(destino)}: {(verificado ? "verificado" : "FALLÓ")}.");
        if (!verificado)
        {
            return (false, "No se pudo iniciar la emulación del mando.");
        }
        Sonar(destino);
        string oculto = MotorControlador.MandoRealOculto ? " El original quedó oculto: los juegos solo ven el emulado."
            : HidHideServicio.Disponible ? "" : " Instala HidHide (Control) para ocultar el original.";
        return (true, $"Tu mando ahora funciona como {Nombre(destino)}.{oculto}");
    }

    public static string Nombre(TipoVirtual? t) => t switch
    {
        TipoVirtual.DualShock4 => "mando de PS4 (DualShock 4)",
        TipoVirtual.Xbox360 => "mando de Xbox 360",
        _ => "mando normal",
    };

    public static TipoVirtual? DesdeTexto(string? valor) => valor switch
    {
        "DualShock4" => TipoVirtual.DualShock4,
        "Xbox360" => TipoVirtual.Xbox360,
        _ => null,
    };

    /// <summary>Sonidos de Windows, distintos para cada caso (solo tras un cambio verificado).</summary>
    private static void Sonar(TipoVirtual? destino)
    {
        if (!ConfigCentro.SonidosMandos)
        {
            return;
        }
        string archivo = destino switch
        {
            TipoVirtual.DualShock4 => "Windows Hardware Insert.wav",   // PS4: "conectar" (dos tonos ascendentes)
            TipoVirtual.Xbox360 => "Windows Unlock.wav",               // Xbox: tono claramente distinto
            _ => "Windows Hardware Remove.wav",                        // apagado: tonos descendentes
        };
        try
        {
            string ruta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media", archivo);
            if (File.Exists(ruta))
            {
                using var reproductor = new SoundPlayer(ruta);
                reproductor.Play();
                return;
            }
        }
        catch { /* sin audio */ }
        (destino is null ? SystemSounds.Hand : destino == TipoVirtual.DualShock4 ? SystemSounds.Asterisk : SystemSounds.Exclamation).Play();
    }
}
