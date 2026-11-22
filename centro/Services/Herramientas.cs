using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CentroAether.Services;

/// <summary>
/// Herramientas que tenía la versión clásica de Aether Toolbox, ahora en la nativa: reparador
/// de red en consola visible (pregunta cosas), copia del reparador para la otra PC, limpieza
/// de Windows, "¿Qué ocupa espacio?", Google en Edge, abrir Windhawk y la revisión completa.
/// </summary>
public static class Herramientas
{
    private static string Windows => Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    private static string CarpetaAether => Path.Combine(Windows, "AtlasModules", "Aether");
    public static string Reparador => Path.Combine(CarpetaAether, "Red", "Reparar-Red-Aether.ps1");

    /// <summary>Abre un script de PowerShell en una consola visible (para los que hacen preguntas).</summary>
    public static bool AbrirConsola(string script, string argumentos = "")
    {
        if (!File.Exists(script))
        {
            return false;
        }
        try
        {
            Process.Start(new ProcessStartInfo("powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" {argumentos}".TrimEnd())
            { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool AbrirReparador(string? equipo = null) =>
        AbrirConsola(Reparador, string.IsNullOrWhiteSpace(equipo) ? "" : $"-Equipo \"{equipo.Replace("\"", "")}\"");

    /// <summary>Copia el reparador al Escritorio para llevarlo a la otra PC. Devuelve la carpeta.</summary>
    public static string GuardarReparadorEnEscritorio()
    {
        string destino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Reparar-Red-Aether");
        Directory.CreateDirectory(destino);
        foreach (string f in Directory.GetFiles(Path.Combine(CarpetaAether, "Red")))
        {
            File.Copy(f, Path.Combine(destino, Path.GetFileName(f)), true);
        }
        return destino;
    }

    public static Task<string> ReconectarUnidades() =>
        Aether.Ejecutar("$f = @(Connect-AetherUnidadesRed); if ($f.Count) { 'FALTAN|' + ($f -join '; ') } else { 'OK' }");

    /// <summary>Temporales del sistema, descargas de Windows Update y caché de Optimización de distribución.</summary>
    public static Task<string> LimpiezaWindows() =>
        Aether.Ejecutar(
            "Remove-Item -Path $env:windir\\Temp\\* -Recurse -Force -ErrorAction SilentlyContinue; " +
            "Stop-Service wuauserv -Force -ErrorAction SilentlyContinue; " +
            "Remove-Item -Path $env:windir\\SoftwareDistribution\\Download\\* -Recurse -Force -ErrorAction SilentlyContinue; " +
            "Start-Service wuauserv -ErrorAction SilentlyContinue; " +
            "Delete-DeliveryOptimizationCache -Force -ErrorAction SilentlyContinue; 'OK'");

    /// <summary>Tamaño de las carpetas que más suelen llenar el disco.</summary>
    public static Task<string> QueOcupaEspacio() =>
        Aether.Ejecutar(
            "$d = Get-PSDrive -Name ($env:SystemDrive.TrimEnd(':')); " +
            "'Disco {0}: {1:N1} GB libres de {2:N1} GB' -f $env:SystemDrive, ($d.Free / 1GB), (($d.Free + $d.Used) / 1GB); ''; " +
            "$c = @('Desktop','Downloads','Documents','Pictures','Videos','Music','AppData') | ForEach-Object { Join-Path $env:USERPROFILE $_ }; " +
            "$c += @(\"$env:windir\\SoftwareDistribution\", \"$env:windir\\Temp\", \"${env:ProgramFiles(x86)}\\Steam\\steamapps\"); " +
            "$r = foreach ($x in $c) { if (-not (Test-Path -LiteralPath $x)) { continue }; $t = 0; " +
            "Get-ChildItem -LiteralPath $x -Recurse -Force -File -ErrorAction SilentlyContinue | ForEach-Object { $t += $_.Length }; " +
            "[pscustomobject]@{ C = $x; GB = [Math]::Round($t / 1GB, 2) } }; " +
            "$r | Sort-Object GB -Descending | ForEach-Object { '{0,8:N2} GB   {1}' -f $_.GB, $_.C }");

    public static void EdgeGoogle()
    {
        try { Process.Start(new ProcessStartInfo("msedge.exe", "edge://settings/searchEngines") { UseShellExecute = true }); }
        catch { /* sin Edge */ }
    }

    public static bool AbrirWindhawk()
    {
        foreach (string? b in new[] { Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramFiles(x86)") })
        {
            if (string.IsNullOrEmpty(b))
            {
                continue;
            }
            string exe = Path.Combine(b, "Windhawk", "windhawk.exe");
            if (File.Exists(exe))
            {
                // Con explorer.exe se abre sin privilegios, como lo abriría el usuario.
                Process.Start("explorer.exe", "\"" + exe + "\"");
                return true;
            }
        }
        return false;
    }

    public static bool RevisionCompleta()
    {
        string cmd = Path.Combine(Windows, "AtlasDesktop", "9. Solucion de problemas", "Revisar la salud de Aether.cmd");
        if (!File.Exists(cmd))
        {
            return false;
        }
        try
        {
            Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"\"{cmd}\"\"") { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
