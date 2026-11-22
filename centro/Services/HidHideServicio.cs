using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CentroAether.Services;

/// <summary>
/// Oculta el mando REAL mientras se emula, para que los juegos y Steam vean solo el mando
/// virtual (Xbox 360 o DualShock 4) y no dos. Usa HidHide (Nefarius, el mismo de DS4Windows)
/// con su herramienta de línea de comandos. Aether Toolbox se registra como app permitida,
/// así que él sí sigue leyendo el mando real. Al detener la emulación (o si Aether Toolbox se
/// cerró de golpe la vez anterior) el mando vuelve a quedar visible.
/// </summary>
public static class HidHideServicio
{
    private static readonly string Registro = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Aether", "hidhide-ocultos.txt");

    public static string? Cli
    {
        get
        {
            foreach (string baseDir in new[]
                     {
                         Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                         Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                     })
            {
                if (string.IsNullOrEmpty(baseDir))
                {
                    continue;
                }
                string ruta = Path.Combine(baseDir, "Nefarius Software Solutions", "HidHide", "x64", "HidHideCLI.exe");
                if (File.Exists(ruta))
                {
                    return ruta;
                }
            }
            return null;
        }
    }

    public static bool Disponible => Cli is not null;

    /// <summary>
    /// Convierte la ruta HID de Windows (\\?\hid#vid_054c&amp;pid_09cc#7&amp;...#{guid}) en la
    /// ruta de instancia que usa HidHide (HID\VID_054C&amp;PID_09CC\7&amp;...).
    /// </summary>
    public static string RutaInstancia(string rutaHid)
    {
        string s = rutaHid;
        if (s.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            s = s[4..];
        }
        int guid = s.LastIndexOf("#{", StringComparison.Ordinal);
        if (guid > 0)
        {
            s = s[..guid];
        }
        return s.Replace('#', '\\').ToUpperInvariant();
    }

    /// <summary>Oculta ese mando a todo el sistema menos a Aether Toolbox.</summary>
    public static bool Ocultar(string rutaHid)
    {
        string? cli = Cli;
        if (cli is null)
        {
            return false;
        }
        string instancia = RutaInstancia(rutaHid);
        string yo = Environment.ProcessPath ?? "";
        bool ok = Correr(cli, "--app-reg", yo)
                  & Correr(cli, "--dev-hide", instancia)
                  & Correr(cli, "--cloak-on");
        Anotar(instancia, agregar: true);
        return ok;
    }

    /// <summary>Vuelve a mostrar ese mando.</summary>
    public static void Mostrar(string rutaHid) => MostrarInstancia(RutaInstancia(rutaHid));

    /// <summary>Al iniciar: muestra los mandos que quedaron ocultos si la app se cerró de golpe.</summary>
    public static void RestaurarPendientes()
    {
        foreach (string instancia in Leer())
        {
            MostrarInstancia(instancia);
        }
    }

    private static void MostrarInstancia(string instancia)
    {
        string? cli = Cli;
        if (cli is not null)
        {
            Correr(cli, "--dev-unhide", instancia);
        }
        Anotar(instancia, agregar: false);
    }

    private static bool Correr(string cli, params string[] argumentos)
    {
        try
        {
            var psi = new ProcessStartInfo(cli) { UseShellExecute = false, CreateNoWindow = true };
            foreach (string a in argumentos)
            {
                psi.ArgumentList.Add(a);
            }
            using Process? p = Process.Start(psi);
            if (p is null)
            {
                return false;
            }
            p.WaitForExit(10000);
            return p.HasExited && p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static List<string> Leer()
    {
        try
        {
            return File.Exists(Registro)
                ? File.ReadAllLines(Registro).Select(l => l.Trim()).Where(l => l.Length > 0).Distinct().ToList()
                : new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static void Anotar(string instancia, bool agregar)
    {
        try
        {
            List<string> lista = Leer();
            lista.RemoveAll(l => string.Equals(l, instancia, StringComparison.OrdinalIgnoreCase));
            if (agregar)
            {
                lista.Add(instancia);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Registro)!);
            File.WriteAllLines(Registro, lista);
        }
        catch { /* sin permisos: no es crítico */ }
    }
}
