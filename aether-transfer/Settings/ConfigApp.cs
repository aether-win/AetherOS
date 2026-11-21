using System;
using System.IO;
using System.Text.Json;

namespace AetherTransfer.Settings;

/// <summary>Configuración de la aplicación, en %APPDATA%\Aether Transfer\config.json.</summary>
public static class ConfigApp
{
    public sealed class Datos
    {
        public int TransferenciasSimultaneas { get; set; } = 2;
        public int LimiteVelocidadKBps { get; set; }      // 0 = sin límite
        public int TimeoutSegundos { get; set; } = 15;
        public bool ConfirmarEliminar { get; set; } = true;
    }

    private static string Carpeta =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Aether Transfer");

    private static string Ruta => Path.Combine(Carpeta, "config.json");
    private static readonly JsonSerializerOptions Opc = new() { WriteIndented = true };

    private static Datos? _cache;

    public static Datos Actual
    {
        get
        {
            if (_cache is not null)
            {
                return _cache;
            }
            try
            {
                if (File.Exists(Ruta))
                {
                    _cache = JsonSerializer.Deserialize<Datos>(File.ReadAllText(Ruta));
                }
            }
            catch { /* por defecto */ }
            return _cache ??= new Datos();
        }
    }

    public static void Guardar(Datos d)
    {
        _cache = d;
        try
        {
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Ruta, JsonSerializer.Serialize(d, Opc));
        }
        catch { /* best-effort */ }
    }
}
