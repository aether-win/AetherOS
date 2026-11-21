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
}
