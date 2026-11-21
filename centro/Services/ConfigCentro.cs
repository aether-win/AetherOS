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
}
