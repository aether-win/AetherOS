using System.Collections.Generic;

namespace CentroAether.Services;

/// <summary>Una app del catálogo del Software Center (se instala vía winget).</summary>
public sealed class AppCatalogo
{
    public string Nombre { get; init; } = "";
    public string Descripcion { get; init; } = "";
    public string WingetId { get; init; } = "";
    public string Categoria { get; init; } = "";
    public string Glyph { get; init; } = "";
}

/// <summary>
/// Catálogo curado de aplicaciones confiables, instalables desde winget (fuentes
/// oficiales). Nada de software pirata ni de terceros no confiables.
/// </summary>
public static class CatalogoApps
{
    public static IReadOnlyList<AppCatalogo> Todas { get; } = new List<AppCatalogo>
    {
        // Esenciales
        new() { Nombre = "7-Zip", Descripcion = "Compresor de archivos ligero y gratuito.", WingetId = "7zip.7zip", Categoria = "Esenciales", Glyph = "" },
        new() { Nombre = "PowerToys", Descripcion = "Utilidades avanzadas de Microsoft para Windows.", WingetId = "Microsoft.PowerToys", Categoria = "Esenciales", Glyph = "" },
        new() { Nombre = "PC Manager", Descripcion = "Herramienta oficial de limpieza y mantenimiento.", WingetId = "Microsoft.PCManager", Categoria = "Esenciales", Glyph = "" },
        new() { Nombre = "Windhawk", Descripcion = "Motor de mods de Windows (restaura tus mods con el respaldo de Aether).", WingetId = "RamenSoftware.Windhawk", Categoria = "Esenciales", Glyph = "" },

        // Navegadores
        new() { Nombre = "Google Chrome", Descripcion = "Navegador de Google.", WingetId = "Google.Chrome", Categoria = "Navegadores", Glyph = "" },
        new() { Nombre = "Brave", Descripcion = "Navegador centrado en la privacidad.", WingetId = "Brave.Brave", Categoria = "Navegadores", Glyph = "" },

        // Juegos
        new() { Nombre = "Steam", Descripcion = "Tienda y biblioteca de juegos de Valve.", WingetId = "Valve.Steam", Categoria = "Juegos", Glyph = "" },
        new() { Nombre = "Epic Games", Descripcion = "Lanzador de la tienda de Epic.", WingetId = "EpicGames.EpicGamesLauncher", Categoria = "Juegos", Glyph = "" },
        new() { Nombre = "Discord", Descripcion = "Chat de voz y texto para jugar.", WingetId = "Discord.Discord", Categoria = "Juegos", Glyph = "" },

        // Multimedia
        new() { Nombre = "VLC", Descripcion = "Reproductor que abre casi cualquier formato.", WingetId = "VideoLAN.VLC", Categoria = "Multimedia", Glyph = "" },
        new() { Nombre = "OBS Studio", Descripcion = "Grabación y transmisión de pantalla.", WingetId = "OBSProject.OBSStudio", Categoria = "Multimedia", Glyph = "" },
        new() { Nombre = "Spotify", Descripcion = "Música en streaming.", WingetId = "Spotify.Spotify", Categoria = "Multimedia", Glyph = "" },

        // Desarrollo
        new() { Nombre = "Visual Studio Code", Descripcion = "Editor de código de Microsoft.", WingetId = "Microsoft.VisualStudioCode", Categoria = "Desarrollo", Glyph = "" },
        new() { Nombre = "Git", Descripcion = "Control de versiones.", WingetId = "Git.Git", Categoria = "Desarrollo", Glyph = "" },
        new() { Nombre = "Notepad++", Descripcion = "Editor de texto rápido y potente.", WingetId = "Notepad++.Notepad++", Categoria = "Desarrollo", Glyph = "" },
    };
}
