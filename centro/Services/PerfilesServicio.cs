using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CentroAether.Services;

/// <summary>Un perfil que asocia una app/juego a un modo de rendimiento y/o a un mando.</summary>
public sealed class PerfilApp
{
    public string App { get; set; } = "";
    public string Proceso { get; set; } = "";   // nombre del .exe para la detección automática
    public string Ruta { get; set; } = "";      // ruta del .exe (informativa)
    public string Modo { get; set; } = "Gaming"; // "Gaming", "Trabajo", "Bateria" o "" (no cambiar)
    public string MandoVirtual { get; set; } = "";   // "", "Xbox360" o "DualShock4"
    public bool Activo { get; set; } = true;          // pausar un perfil sin borrarlo
}

/// <summary>
/// Guarda y lee los perfiles por aplicación en un JSON propio del Centro
/// (ProgramData\Aether\perfiles.json). La aplicación automática al abrir la app
/// llegará con Background Intelligence; aquí se define el qué.
/// </summary>
public static class PerfilesServicio
{
    private static string Carpeta =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Aether");

    private static string Ruta => Path.Combine(Carpeta, "perfiles.json");

    private static readonly JsonSerializerOptions Opciones = new() { WriteIndented = true };

    public static List<PerfilApp> Cargar()
    {
        try
        {
            if (File.Exists(Ruta))
            {
                return JsonSerializer.Deserialize<List<PerfilApp>>(File.ReadAllText(Ruta)) ?? new List<PerfilApp>();
            }
        }
        catch { /* archivo ilegible: lista vacía */ }
        return new List<PerfilApp>();
    }

    public static bool Guardar(List<PerfilApp> perfiles)
    {
        try
        {
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Ruta, JsonSerializer.Serialize(perfiles, Opciones));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
