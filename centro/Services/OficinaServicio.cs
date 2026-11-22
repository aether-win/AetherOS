using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CentroAether.Services;

/// <summary>
/// Protección de Office de Aether:
///  1. Blindaje contra mods: Word, Excel, PowerPoint, Outlook, OneNote, Access, Publisher,
///     Visio y Project van en la lista de exclusión GLOBAL de Windhawk
///     (HKLM\SOFTWARE\Windhawk\Engine\Settings, valor "Exclude"). Un programa excluido ahí
///     queda totalmente fuera de Windhawk: ningún mod, actual o futuro, lo puede alterar
///     (transparencias, vidrio, colores…). Aether Toolbox lo vigila y lo repone si se pierde.
///  2. Macros sin bloqueo y sin avisos de otra PC (las mismas directivas que el instalador:
///     tweak misc/aether-office-macros).
///  3. Desbloquear archivos que ya llegaron marcados "de otra PC/Internet".
/// </summary>
public static class OficinaServicio
{
    /// <summary>Procesos de Office que ningún mod debe tocar.</summary>
    public static readonly string[] ProcesosOffice =
    {
        "WINWORD.EXE", "EXCEL.EXE", "POWERPNT.EXE", "OUTLOOK.EXE", "olk.exe",
        "ONENOTE.EXE", "ONENOTEM.EXE", "MSACCESS.EXE", "MSPUB.EXE", "VISIO.EXE", "WINPROJ.EXE",
    };

    private const string ClaveWindhawk = @"SOFTWARE\Windhawk\Engine\Settings";

    private static RegistryKey Hklm => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
    private static RegistryKey Hkcu => RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

    public static bool OfficeInstalado()
    {
        try
        {
            using RegistryKey? k = Hklm.OpenSubKey(@"SOFTWARE\Microsoft\Office\16.0");
            if (k is not null)
            {
                return true;
            }
            using RegistryKey? w = Hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\winword.exe");
            return w is not null;
        }
        catch
        {
            return false;
        }
    }

    // ── 1. Blindaje contra mods de Windhawk ──────────────────────────────────────────

    public static bool WindhawkInstalado()
    {
        try
        {
            using RegistryKey? k = Hklm.OpenSubKey(@"SOFTWARE\Windhawk");
            return k is not null
                || File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk.exe"));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>True si todos los procesos de Office están en la exclusión global de Windhawk.</summary>
    public static bool OfficeBlindado()
    {
        List<string> lista = LeerExclusiones(out _, out _);
        return ProcesosOffice.All(p => lista.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Agrega Office a la exclusión global de Windhawk (respetando lo que ya hubiera) y reinicia
    /// Windhawk para que aplique. Devuelve true si quedó blindado.
    /// </summary>
    public static bool BlindarOffice()
    {
        try
        {
            List<string> lista = LeerExclusiones(out string separador, out bool multi);
            var faltan = ProcesosOffice.Where(p => !lista.Contains(p, StringComparer.OrdinalIgnoreCase)).ToList();
            if (faltan.Count == 0)
            {
                return true;
            }
            lista.AddRange(faltan);

            using RegistryKey k = Hklm.CreateSubKey(ClaveWindhawk);
            if (multi)
            {
                k.SetValue("Exclude", lista.ToArray(), RegistryValueKind.MultiString);
            }
            else
            {
                k.SetValue("Exclude", string.Join(separador, lista), RegistryValueKind.String);
            }

            ReiniciarWindhawk();
            return OfficeBlindado();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Lee la lista "Exclude" de Windhawk. Respeta el formato que ya tenga: texto separado por
    /// "|" (el de Windhawk), por saltos de línea, o lista múltiple del registro.
    /// </summary>
    private static List<string> LeerExclusiones(out string separador, out bool multi)
    {
        separador = "|";
        multi = false;
        try
        {
            using RegistryKey? k = Hklm.OpenSubKey(ClaveWindhawk);
            object? valor = k?.GetValue("Exclude");
            if (valor is string[] varias)
            {
                multi = true;
                return varias.Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            }
            string texto = valor as string ?? "";
            if (!texto.Contains('|') && (texto.Contains('\n') || texto.Contains('\r')))
            {
                separador = "\r\n";
            }
            return texto.Split(new[] { '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>Reinicia el servicio y la app de Windhawk para que lea la nueva lista.</summary>
    private static void ReiniciarWindhawk()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-NonInteractive");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add(
                "$s = Get-Service -Name 'Windhawk' -ErrorAction SilentlyContinue; " +
                "if ($s) { Restart-Service -Name 'Windhawk' -Force -ErrorAction SilentlyContinue }");
            using Process? p = Process.Start(psi);
            p?.WaitForExit(20000);
        }
        catch
        {
            // Si no se pudo reiniciar, aplicará al próximo inicio de Windhawk.
        }
    }

    // ── 2. Macros sin bloqueo (igual que el instalador) ──────────────────────────────

    private static readonly (string App, bool VistaProtegidaAdjuntos)[] AppsMacros =
    {
        ("Excel", true), ("Word", false), ("PowerPoint", false),
    };

    public static bool MacrosSinBloqueo()
    {
        try
        {
            foreach ((string app, _) in AppsMacros)
            {
                using RegistryKey? k = Hkcu.OpenSubKey($@"Software\Policies\Microsoft\Office\16.0\{app}\Security");
                if (k?.GetValue("VBAWarnings") is not int v || v != 1)
                {
                    return false;
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Mismas directivas que el tweak misc/aether-office-macros.</summary>
    public static bool AplicarMacrosSinBloqueo()
    {
        try
        {
            foreach ((string app, bool adjuntos) in AppsMacros)
            {
                using (RegistryKey k = Hkcu.CreateSubKey($@"Software\Policies\Microsoft\Office\16.0\{app}\Security"))
                {
                    k.SetValue("VBAWarnings", 1, RegistryValueKind.DWord);
                    k.SetValue("blockcontentexecutionfrominternet", 0, RegistryValueKind.DWord);
                }
                if (app == "PowerPoint")
                {
                    continue;
                }
                using RegistryKey pv = Hkcu.CreateSubKey($@"Software\Policies\Microsoft\Office\16.0\{app}\Security\ProtectedView");
                pv.SetValue("DisableInternetFilesInPV", 1, RegistryValueKind.DWord);
                pv.SetValue("DisableUnsafeLocationsInPV", 1, RegistryValueKind.DWord);
                if (adjuntos)
                {
                    pv.SetValue("DisableAttachmentsInPV", 1, RegistryValueKind.DWord);
                }
            }
            return MacrosSinBloqueo();
        }
        catch
        {
            return false;
        }
    }

    // ── 3. Desbloquear archivos de otra PC ───────────────────────────────────────────

    private static readonly string[] ExtensionesOffice =
    {
        ".doc", ".docx", ".docm", ".dot", ".dotx", ".dotm", ".rtf",
        ".xls", ".xlsx", ".xlsm", ".xlsb", ".xlt", ".xltx", ".xltm", ".xlam", ".xla", ".csv",
        ".ppt", ".pptx", ".pptm", ".pot", ".potx", ".potm", ".pps", ".ppsx", ".ppsm", ".ppam",
        ".accdb", ".mdb", ".pub", ".vsd", ".vsdx", ".mpp", ".one",
    };

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool DeleteFileW(string ruta);

    /// <summary>
    /// Quita la marca "vino de otra PC/Internet" (Zone.Identifier) de los archivos de Office de
    /// una carpeta y sus subcarpetas. Devuelve cuántos desbloqueó.
    /// </summary>
    public static int DesbloquearCarpeta(string carpeta)
    {
        int cuenta = 0;
        var opciones = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };
        try
        {
            foreach (string archivo in Directory.EnumerateFiles(carpeta, "*", opciones))
            {
                string ext = Path.GetExtension(archivo);
                if (!ExtensionesOffice.Contains(ext, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (DeleteFileW(archivo + ":Zone.Identifier"))
                {
                    cuenta++;
                }
            }
        }
        catch
        {
            // carpeta sin acceso: se devuelve lo que se alcanzó
        }
        return cuenta;
    }
}
