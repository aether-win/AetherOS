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

    // --- Software / Runtimes (winget) --------------------------------------------------
    /// <summary>Instala un paquete por su Id de winget, de forma silenciosa.</summary>
    public static Task<string> InstalarWinget(string id) =>
        CorrerPowerShell(
            $"winget install --id {id} -e --accept-source-agreements --accept-package-agreements --disable-interactivity");

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

    // --- Limpieza ----------------------------------------------------------------------
    public static Task<string> Limpiar() => Ejecutar("Clear-AetherTemporales");

    public static Task<string> AbrirAlmacenamiento() =>
        CorrerPowerShell("Start-Process 'ms-settings:storagesense'");

    // --- Bateria y salud ---------------------------------------------------------------
    public static async Task<string> Bateria()
    {
        string s = (await Ejecutar("$b=Get-AetherBateria; if($null -eq $b){'SIN'}else{$b}")).Trim();
        return s;
    }

    public static async Task<string> SaludBateria()
    {
        string s = (await Ejecutar(
            "$h=Get-AetherSaludBateria; if($null -eq $h){'SIN'}else{\"$($h.Salud)|$($h.Actual)|$($h.Diseno)\"}")).Trim();
        return s;
    }

    // --- Ajustes (HKCU\Software\AetherOS\Centro) ---------------------------------------
    public static async Task<string> LeerAjuste(string nombre, string porDefecto)
    {
        string s = (await Ejecutar($"Get-AetherAjuste -Nombre '{nombre}' -PorDefecto '{porDefecto}'")).Trim();
        return string.IsNullOrEmpty(s) ? porDefecto : s;
    }

    public static Task<string> GuardarAjuste(string nombre, int valor) =>
        Ejecutar($"Set-AetherAjuste -Nombre '{nombre}' -Valor {valor}");

    // --- Tema --------------------------------------------------------------------------
    public static Task<string> AplicarTema(string tema) => Ejecutar($"Set-AetherTema -Tema {tema}");

    // --- Respaldo ----------------------------------------------------------------------
    public static Task<string> ConfigurarRespaldo(string destino)
    {
        string esc = destino.Replace("'", "''");
        return CorrerPowerShell(
            "$d=Join-Path $env:APPDATA 'Aether'; New-Item -ItemType Directory -Path $d -Force | Out-Null; " +
            "$f=Join-Path $d 'respaldo.json'; " +
            "$c=if(Test-Path $f){Get-Content $f -Raw | ConvertFrom-Json}else{[pscustomobject]@{}}; " +
            $"$c | Add-Member -NotePropertyName Destino -NotePropertyValue '{esc}' -Force; " +
            "$c | ConvertTo-Json | Set-Content -LiteralPath $f -Encoding UTF8");
    }

    public static Task<string> HacerRespaldo()
    {
        string script = Path.Combine(CarpetaAether, "Centro", "Respaldo-Aether.ps1");
        return CorrerPowerShell($"& '{script.Replace("'", "''")}'");
    }

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
