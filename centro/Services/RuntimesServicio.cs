using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace CentroAether.Services;

/// <summary>Estado de un runtime del sistema.</summary>
public sealed class RuntimeInfo
{
    public string Clave { get; init; } = "";
    public string Nombre { get; init; } = "";
    public string Descripcion { get; init; } = "";
    public string WingetId { get; init; } = "";
    public string Version { get; set; } = "";
    public bool Instalado => !string.IsNullOrWhiteSpace(Version);
}

/// <summary>
/// Detecta los runtimes instalados leyendo el registro y el disco de forma nativa
/// (solo lectura). La instalación usa los paquetes OFICIALES de Microsoft vía winget.
/// </summary>
public static class RuntimesServicio
{
    public static List<RuntimeInfo> Detectar()
    {
        return new List<RuntimeInfo>
        {
            new()
            {
                Clave = "vcredist",
                Nombre = "Visual C++ 2015-2022",
                Descripcion = "Librerías que muchos juegos y programas necesitan.",
                WingetId = "Microsoft.VCRedist.2015+.x64",
                Version = VcRedist(),
            },
            new()
            {
                Clave = "netfx",
                Nombre = ".NET Framework",
                Descripcion = "Plataforma clásica de Windows (4.x).",
                WingetId = "Microsoft.DotNet.Framework.DeveloperPack_4",
                Version = NetFramework(),
            },
            new()
            {
                Clave = "net8",
                Nombre = ".NET Desktop Runtime 8",
                Descripcion = "Runtime moderno para apps de escritorio.",
                WingetId = "Microsoft.DotNet.DesktopRuntime.8",
                Version = NetDesktop8(),
            },
            new()
            {
                Clave = "webview2",
                Nombre = "WebView2",
                Descripcion = "Motor web que usan muchas apps (incluido Aether).",
                WingetId = "Microsoft.EdgeWebView2Runtime",
                Version = WebView2(),
            },
            new()
            {
                Clave = "directx",
                Nombre = "DirectX (End-User Runtime)",
                Descripcion = "Librerías gráficas heredadas para juegos.",
                WingetId = "Microsoft.DirectX",
                Version = DirectX(),
            },
            new()
            {
                Clave = "python",
                Nombre = "Python 3",
                Descripcion = "Intérprete de Python para scripts y herramientas.",
                WingetId = "Python.Python.3.13",
                Version = Python(),
            },
        };
    }

    private static RegistryKey Hklm64 =>
        RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

    private static RegistryKey Hkcu =>
        RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

    private static string VcRedist()
    {
        try
        {
            using RegistryKey? k = Hklm64.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64");
            if (k is null)
            {
                return "";
            }
            object? inst = k.GetValue("Installed");
            if (inst is int i && i == 1)
            {
                return k.GetValue("Version")?.ToString()?.TrimStart('v') ?? "instalado";
            }
        }
        catch { /* no instalado */ }
        return "";
    }

    private static string NetFramework()
    {
        try
        {
            using RegistryKey? k = Hklm64.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full");
            if (k?.GetValue("Release") is int r)
            {
                if (r >= 533320)
                {
                    return "4.8.1";
                }
                if (r >= 528040)
                {
                    return "4.8";
                }
                return "4.x";
            }
        }
        catch { /* no instalado */ }
        return "";
    }

    private static string NetDesktop8()
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "dotnet", "shared", "Microsoft.WindowsDesktop.App");
            if (Directory.Exists(dir))
            {
                string? v = Directory.GetDirectories(dir)
                    .Select(Path.GetFileName)
                    .Where(n => n is not null && n.StartsWith("8.", StringComparison.Ordinal))
                    .OrderByDescending(n => n)
                    .FirstOrDefault();
                return v ?? "";
            }
        }
        catch { /* no instalado */ }
        return "";
    }

    private static string WebView2()
    {
        const string sub = @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";
        try
        {
            using (RegistryKey? k = Hklm64.OpenSubKey(sub))
            {
                string? pv = k?.GetValue("pv")?.ToString();
                if (!string.IsNullOrEmpty(pv) && pv != "0.0.0.0")
                {
                    return pv;
                }
            }
            using (RegistryKey? ku = Hkcu.OpenSubKey(
                @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"))
            {
                string? pv = ku?.GetValue("pv")?.ToString();
                if (!string.IsNullOrEmpty(pv) && pv != "0.0.0.0")
                {
                    return pv;
                }
            }
        }
        catch { /* no instalado */ }
        return "";
    }

    private static string DirectX()
    {
        try
        {
            string dll = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System), "d3dx9_43.dll");
            return File.Exists(dll) ? "instalado" : "";
        }
        catch { return ""; }
    }

    private static string Python()
    {
        foreach (RegistryKey baseKey in new[] { Hklm64, Hkcu })
        {
            try
            {
                using RegistryKey? core = baseKey.OpenSubKey(@"SOFTWARE\Python\PythonCore");
                string? v = core?.GetSubKeyNames().OrderByDescending(n => n).FirstOrDefault();
                if (!string.IsNullOrEmpty(v))
                {
                    return v;
                }
            }
            catch { /* siguiente */ }
        }
        return "";
    }
}
