using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace CentroAether.Services;

/// <summary>Un juego detectado en la biblioteca.</summary>
public sealed class JuegoInfo
{
    public string AppId { get; init; } = "";
    public string Nombre { get; init; } = "";
    public string Tienda { get; init; } = "Steam";
}

/// <summary>
/// Detecta juegos instalados leyendo la configuración de Steam (registro + archivos
/// .acf de las bibliotecas). Solo lectura. Base del módulo Game Library.
/// </summary>
public static partial class BibliotecaServicio
{
    public static List<JuegoInfo> DetectarSteam()
    {
        var juegos = new List<JuegoInfo>();
        string? steam = RutaSteam();
        if (steam is null)
        {
            return juegos;
        }

        foreach (string biblioteca in Bibliotecas(steam))
        {
            string apps = Path.Combine(biblioteca, "steamapps");
            if (!Directory.Exists(apps))
            {
                continue;
            }

            foreach (string acf in SafeEnum(apps, "appmanifest_*.acf"))
            {
                try
                {
                    string texto = File.ReadAllText(acf);
                    string id = Campo(texto, "appid");
                    string nombre = Campo(texto, "name");
                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(nombre))
                    {
                        juegos.Add(new JuegoInfo { AppId = id, Nombre = nombre });
                    }
                }
                catch { /* manifiesto ilegible: siguiente */ }
            }
        }

        return juegos
            .GroupBy(j => j.AppId)
            .Select(g => g.First())
            .OrderBy(j => j.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static string? RutaSteam()
    {
        try
        {
            using (RegistryKey? k = RegistryKey
                .OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
                .OpenSubKey(@"SOFTWARE\Valve\Steam"))
            {
                string? p = k?.GetValue("SteamPath")?.ToString();
                if (!string.IsNullOrEmpty(p) && Directory.Exists(p))
                {
                    return p;
                }
            }
            using (RegistryKey? k = RegistryKey
                .OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                .OpenSubKey(@"SOFTWARE\Valve\Steam"))
            {
                string? p = k?.GetValue("InstallPath")?.ToString();
                if (!string.IsNullOrEmpty(p) && Directory.Exists(p))
                {
                    return p;
                }
            }
        }
        catch { /* sin Steam */ }
        return null;
    }

    private static IEnumerable<string> Bibliotecas(string steam)
    {
        var rutas = new List<string> { steam };
        string vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdf))
        {
            try
            {
                string texto = File.ReadAllText(vdf);
                foreach (Match m in RegexRutaBiblioteca().Matches(texto))
                {
                    string ruta = m.Groups[1].Value.Replace(@"\\", @"\");
                    if (Directory.Exists(ruta))
                    {
                        rutas.Add(ruta);
                    }
                }
            }
            catch { /* usamos solo la principal */ }
        }
        return rutas.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SafeEnum(string carpeta, string patron)
    {
        try
        {
            return Directory.EnumerateFiles(carpeta, patron);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string Campo(string texto, string clave)
    {
        Match m = Regex.Match(texto, "\"" + Regex.Escape(clave) + "\"\\s+\"([^\"]*)\"");
        return m.Success ? m.Groups[1].Value : "";
    }

    [GeneratedRegex("\"path\"\\s+\"([^\"]+)\"")]
    private static partial Regex RegexRutaBiblioteca();
}
