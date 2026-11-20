using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CentroAether.Services;

/// <summary>Un mod de Wind-Aether (Windhawk) para mostrar con casilla.</summary>
public sealed class ModWind
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; }
}

/// <summary>
/// Puente entre la interfaz nativa y la logica de Aether, que ya vive y esta probada en
/// PowerShell (Aether.Centro.ps1). El Centro corre como administrador (ver app.manifest),
/// asi que cada llamada de PowerShell tambien es elevada: sin pedir permisos por accion y
/// sin ventana de consola (se lanza oculta).
/// </summary>
public static class Aether
{
    public static string CentroPs1 =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "AtlasModules", "Aether", "Centro", "Aether.Centro.ps1");

    public static bool Disponible => File.Exists(CentroPs1);

    /// <summary>Carga Aether.Centro.ps1 y ejecuta un comando; devuelve su salida de texto.</summary>
    public static Task<string> Ejecutar(string comando)
    {
        string script = $". '{CentroPs1.Replace("'", "''")}'; {comando}";
        return CorrerPowerShell(script);
    }

    public static Task<string> CambiarModo(string modo) => Ejecutar($"Set-AetherModo -Modo {modo}");

    private static string CarpetaAether =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "AtlasModules", "Aether");

    // --- Actualizaciones ---------------------------------------------------------------
    public static async Task<bool> WindowsUpdateEnPausa()
    {
        string salida = await CorrerPowerShell(
            "$f=(Get-ItemProperty 'HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings' -Name PauseUpdatesExpiryTime -ErrorAction SilentlyContinue).PauseUpdatesExpiryTime; " +
            "if($f -and ([datetime]$f) -gt (Get-Date)){'PAUSADO'}else{'ACTIVO'}");
        return salida.Contains("PAUSADO", StringComparison.OrdinalIgnoreCase);
    }

    public static Task<string> PausarWindowsUpdate(bool pausar)
    {
        string rel = pausar
            ? @"3. Configuracion general\Pausar actualizaciones\Pausar Windows Update (predeterminado).cmd"
            : @"3. Configuracion general\Pausar actualizaciones\Quitar pausa de Windows Update.cmd";
        return Ejecutar($"[void](Invoke-AetherLanzador -Relativa '{rel.Replace("'", "''")}')");
    }

    public static Task<string> ActualizarProgramas() =>
        CorrerPowerShell("winget upgrade --all --accept-source-agreements --accept-package-agreements --disable-interactivity");

    // --- Red ---------------------------------------------------------------------------
    public static Task<string> RepararRed()
    {
        string script = Path.Combine(CarpetaAether, "Red", "Reparar-Red-Aether.ps1");
        return CorrerPowerShell($"& '{script.Replace("'", "''")}'");
    }

    public static Task<string> HacerRedPrivada()
    {
        string script = Path.Combine(CarpetaAether, "Red", "Redes-Privadas.ps1");
        return CorrerPowerShell($"& '{script.Replace("'", "''")}'");
    }

    // --- Wind-Aether (mods) ------------------------------------------------------------
    public static List<ModWind> ListaMods()
    {
        var lista = new List<ModWind>();
        try
        {
            string json = Path.Combine(CarpetaAether, "Wind-Aether", "Wind-Aether-mods.json");
            if (!File.Exists(json)) return lista;
            using var doc = JsonDocument.Parse(File.ReadAllText(json));
            if (doc.RootElement.TryGetProperty("mods", out var mods))
            {
                foreach (var m in mods.EnumerateArray())
                {
                    string id = m.TryGetProperty("modId", out var i) ? (i.GetString() ?? "") : "";
                    string nombre = m.TryGetProperty("name", out var n) ? (n.GetString() ?? id) : id;
                    bool disabled = true;
                    if (m.TryGetProperty("config", out var c) && c.TryGetProperty("disabled", out var d))
                    {
                        disabled = d.ValueKind == JsonValueKind.True;
                    }
                    if (!string.IsNullOrEmpty(id))
                    {
                        lista.Add(new ModWind { Id = id, Nombre = nombre, Activo = !disabled });
                    }
                }
            }
        }
        catch { /* lista vacia */ }
        return lista;
    }

    public static Task<string> ActivarMod(string id, bool activo) =>
        Ejecutar($"[void](Set-AetherModWind -Id '{id.Replace("'", "''")}' -Activo ${(activo ? "true" : "false")})");

    public static bool WindhawkInstalado() =>
        File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk-cli.exe"));

    public static Task<string> AbrirUsoCompartido() =>
        CorrerPowerShell("Start-Process control.exe '/name Microsoft.NetworkAndSharingCenter'");

    public static string NombreEquipo => Environment.MachineName;

    public static Task<string> Limpiar() => Ejecutar("Clear-AetherTemporales");

    public static async Task<string> ModoActual()
    {
        if (!Disponible) return "Trabajo";
        string salida = await Ejecutar("Get-AetherModo");
        string texto = salida.Trim();
        return string.IsNullOrEmpty(texto) ? "Trabajo" : texto;
    }

    private static async Task<string> CorrerPowerShell(string script)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-NonInteractive");
        psi.ArgumentList.Add("-WindowStyle");
        psi.ArgumentList.Add("Hidden");
        psi.ArgumentList.Add("-Command");
        psi.ArgumentList.Add(script);

        try
        {
            using var proc = new Process { StartInfo = psi };
            proc.Start();
            string salida = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return salida;
        }
        catch (Exception ex)
        {
            return "ERROR: " + ex.Message;
        }
    }
}
