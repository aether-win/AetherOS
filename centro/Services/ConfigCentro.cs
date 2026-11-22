using System;
using System.IO;
using System.Text.Json;

namespace CentroAether.Services;

/// <summary>Configuración propia del Centro (ProgramData\Aether\centro-config.json).</summary>
public static class ConfigCentro
{
    private sealed class Datos
    {
        public bool Inteligencia { get; set; }
        public double Deadzone { get; set; } = 0.10;      // 0..1 (zona muerta de sticks)
        public double Sensibilidad { get; set; } = 1.0;   // 0.5..2.0
        public bool MandosActivos { get; set; } = true;   // interruptor general de mandos
        public int ApagadoMandoMin { get; set; }          // 0 = nunca apagar el mando Bluetooth
        public bool SonidosMandos { get; set; } = true;   // sonido al cambiar de mando
        public string MandoPredeterminado { get; set; } = ""; // "", "Xbox360" o "DualShock4"
    }

    private static string Carpeta =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Aether");

    private static string Ruta => Path.Combine(Carpeta, "centro-config.json");

    private static Datos Leer()
    {
        try
        {
            if (File.Exists(Ruta))
            {
                return JsonSerializer.Deserialize<Datos>(File.ReadAllText(Ruta)) ?? new Datos();
            }
        }
        catch { /* valores por defecto */ }
        return new Datos();
    }

    private static void Escribir(Datos d)
    {
        try
        {
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Ruta, JsonSerializer.Serialize(d));
        }
        catch { /* best-effort */ }
    }

    /// <summary>Aplicar perfiles automáticamente en segundo plano. Apagado por defecto.</summary>
    public static bool InteligenciaActiva
    {
        get => Leer().Inteligencia;
        set
        {
            Datos d = Leer();
            d.Inteligencia = value;
            Escribir(d);
        }
    }

    /// <summary>Zona muerta de los sticks del controlador (0..1).</summary>
    public static double Deadzone
    {
        get => Leer().Deadzone;
        set
        {
            Datos d = Leer();
            d.Deadzone = value;
            Escribir(d);
        }
    }

    /// <summary>Sensibilidad de los sticks (0.5..2.0).</summary>
    public static double Sensibilidad
    {
        get => Leer().Sensibilidad;
        set
        {
            Datos d = Leer();
            d.Sensibilidad = value;
            Escribir(d);
        }
    }

    private static T Cambiar<T>(Func<Datos, T> leer, Action<Datos> escribir, bool guardar)
    {
        Datos d = Leer();
        if (guardar)
        {
            escribir(d);
            Escribir(d);
        }
        return leer(d);
    }

    /// <summary>Interruptor general de mandos (si está apagado, nada de mandos corre).</summary>
    public static bool MandosActivos
    {
        get => Leer().MandosActivos;
        set => Cambiar(d => d.MandosActivos, d => d.MandosActivos = value, true);
    }

    /// <summary>Minutos sin uso para apagar un mando Bluetooth (0 = nunca).</summary>
    public static int ApagadoMandoMin
    {
        get => Math.Max(0, Leer().ApagadoMandoMin);
        set => Cambiar(d => d.ApagadoMandoMin, d => d.ApagadoMandoMin = Math.Clamp(value, 0, 600), true);
    }

    /// <summary>Sonido de Windows al cambiar entre mando Xbox y PS4.</summary>
    public static bool SonidosMandos
    {
        get => Leer().SonidosMandos;
        set => Cambiar(d => d.SonidosMandos, d => d.SonidosMandos = value, true);
    }

    /// <summary>Mando al que se vuelve al cerrar un juego con perfil: "", "Xbox360" o "DualShock4".</summary>
    public static string MandoPredeterminado
    {
        get => Leer().MandoPredeterminado ?? "";
        set => Cambiar(d => d.MandoPredeterminado, d => d.MandoPredeterminado = value ?? "", true);
    }
}
