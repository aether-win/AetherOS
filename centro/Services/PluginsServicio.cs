using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CentroAether.Services;

/// <summary>Un plugin declarado por manifiesto JSON.</summary>
public sealed class PluginInfo
{
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Abrir { get; set; } = "";   // URL (https://, steam://, ms-settings:) o ruta a abrir
}

/// <summary>
/// Sistema de plugins (v1, seguro): lee manifiestos JSON desde
/// ProgramData\Aether\Plugins\*.json. Cada plugin solo puede "abrir" una URL o
/// ruta (no ejecuta comandos arbitrarios). La API ampliada llegará más adelante.
/// </summary>
public static class PluginsServicio
{
    public static string Carpeta =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Aether", "Plugins");

    public static void AsegurarCarpeta()
    {
        try
        {
            Directory.CreateDirectory(Carpeta);
        }
        catch { /* best-effort */ }
    }

    public static List<PluginInfo> Listar()
    {
        var lista = new List<PluginInfo>();
        try
        {
            if (!Directory.Exists(Carpeta))
            {
                return lista;
            }
            foreach (string f in Directory.EnumerateFiles(Carpeta, "*.json"))
            {
                try
                {
                    PluginInfo? p = JsonSerializer.Deserialize<PluginInfo>(File.ReadAllText(f));
                    if (p is not null && !string.IsNullOrWhiteSpace(p.Nombre))
                    {
                        lista.Add(p);
                    }
                }
                catch { /* manifiesto inválido: siguiente */ }
            }
        }
        catch { /* sin acceso */ }
        return lista;
    }
}
