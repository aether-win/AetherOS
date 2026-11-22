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

    // ── 4. Office no cambia con el tema de Windows (claro/oscuro) ────────────────────
    // "UI Theme" (HKCU\Software\Microsoft\Office\16.0\Common): 0 Colorido, 3 Gris oscuro,
    // 4 Negro, 5 Blanco, 6 Usar la del sistema. Con 6 Office sigue a Windows (oscuro = Negro), y
    // en Negro Word y Outlook RECOLOREAN el documento: texto lavado o claro. Aether lo deja fijo
    // en un tema que nunca toca el documento (Colorido, Blanco o Gris oscuro).

    private const string ClaveComun = @"Software\Microsoft\Office\16.0\Common";

    public static readonly (int Valor, string Nombre)[] TemasSeguros =
    {
        (0, "Colorido (recomendado)"), (5, "Blanco"), (3, "Gris oscuro"),
    };

    /// <summary>Tema actual de Office (valor local "UI Theme"), o -1 si no está definido (= sigue al sistema).</summary>
    public static int TemaOffice()
    {
        try
        {
            using RegistryKey? k = Hkcu.OpenSubKey(ClaveComun);
            return k?.GetValue("UI Theme") is int v ? v : -1;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// True si Office tiene un tema fijo seguro (no Negro ni "del sistema") en el valor local Y en
    /// la copia de la cuenta (Roaming\...\Settings\1186\{0…0}\Data). Office recarga esa copia
    /// al iniciar y pisa el valor local: si solo se cambia el local, vuelve al tema Negro.
    /// </summary>
    public static bool TemaProtegido()
    {
        if (!TemasSeguros.Any(t => t.Valor == TemaOffice()))
        {
            return false;
        }
        foreach ((RegistryKey clave, int valor) in CopiasDeCuenta())
        {
            using (clave)
            {
                if (!TemasSeguros.Any(t => t.Valor == valor))
                {
                    return false;
                }
            }
        }
        return true;
    }

    public static bool FijarTema(int valor)
    {
        if (!TemasSeguros.Any(t => t.Valor == valor))
        {
            valor = 0;
        }
        try
        {
            using (RegistryKey k = Hkcu.CreateSubKey(ClaveComun))
            {
                k.SetValue("UI Theme", valor, RegistryValueKind.DWord);
                k.SetValue("PendingUITheme", valor, RegistryValueKind.DWord);
            }

            // La copia de la cuenta (la que Office recarga al iniciar), en todas las cuentas.
            foreach ((RegistryKey clave, _) in CopiasDeCuenta(escribible: true))
            {
                using (clave)
                {
                    if (clave.GetValueKind("Data") == RegistryValueKind.Binary)
                    {
                        clave.SetValue("Data", BitConverter.GetBytes(valor), RegistryValueKind.Binary);
                    }
                    else
                    {
                        clave.SetValue("Data", valor, RegistryValueKind.DWord);
                    }
                }
            }

            // Directiva de usuario: refuerzo para que Office no lo cambie por su cuenta.
            using (RegistryKey p = Hkcu.CreateSubKey(@"Software\Policies\Microsoft\Office\16.0\Common"))
            {
                p.SetValue("UI Theme", valor, RegistryValueKind.DWord);
            }
            return TemaProtegido();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Busca la copia del tema de cada cuenta de Office: claves "1186\{00000000-…}" con valor
    /// "Data" bajo HKCU\Software\Microsoft\Office\16.0\Common\Roaming (la ruta cambia según la
    /// cuenta e instalación, por eso se recorre). Devuelve la clave abierta y el tema guardado.
    /// </summary>
    private static List<(RegistryKey Clave, int Valor)> CopiasDeCuenta(bool escribible = false)
    {
        var lista = new List<(RegistryKey, int)>();
        try
        {
            using RegistryKey? raiz = Hkcu.OpenSubKey(ClaveComun + @"\Roaming");
            if (raiz is not null)
            {
                Recorrer(raiz, ClaveComun + @"\Roaming", 0, escribible, lista);
            }
        }
        catch { /* sin copias */ }
        return lista;
    }

    private static void Recorrer(RegistryKey clave, string ruta, int nivel, bool escribible, List<(RegistryKey, int)> lista)
    {
        if (nivel > 6)
        {
            return;
        }
        foreach (string nombre in clave.GetSubKeyNames())
        {
            string sub = ruta + "\\" + nombre;
            try
            {
                if (nombre == "1186")
                {
                    using RegistryKey? k1186 = Hkcu.OpenSubKey(sub);
                    if (k1186 is null)
                    {
                        continue;
                    }
                    foreach (string hijo in k1186.GetSubKeyNames())
                    {
                        if (!hijo.StartsWith("{00000000-0000-0000-0000-000000000000}", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        RegistryKey? dato = Hkcu.OpenSubKey(sub + "\\" + hijo, escribible);
                        object? v = dato?.GetValue("Data");
                        int? tema = v switch
                        {
                            int i => i,
                            byte[] b when b.Length >= 4 => BitConverter.ToInt32(b, 0),
                            _ => null,
                        };
                        if (dato is not null && tema is not null)
                        {
                            lista.Add((dato, tema.Value));
                        }
                        else
                        {
                            dato?.Dispose();
                        }
                    }
                }
                else
                {
                    using RegistryKey? hijoClave = Hkcu.OpenSubKey(sub);
                    if (hijoClave is not null)
                    {
                        Recorrer(hijoClave, sub, nivel + 1, escribible, lista);
                    }
                }
            }
            catch { /* clave sin acceso */ }
        }
    }

    // ── 5. Compatibilidad gráfica (sin aceleración por hardware) ─────────────────────

    private const string ClaveGraficos = @"Software\Microsoft\Office\16.0\Common\Graphics";

    public static bool SinAceleracion()
    {
        try
        {
            using RegistryKey? k = Hkcu.OpenSubKey(ClaveGraficos);
            return k?.GetValue("DisableHardwareAcceleration") is int v && v == 1;
        }
        catch
        {
            return false;
        }
    }

    public static bool FijarSinAceleracion(bool activar)
    {
        try
        {
            using RegistryKey k = Hkcu.CreateSubKey(ClaveGraficos);
            k.SetValue("DisableHardwareAcceleration", activar ? 1 : 0, RegistryValueKind.DWord);
            return SinAceleracion() == activar;
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
