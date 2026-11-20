# Companion de aether-centro.psd1: accesos directos del Centro Aether y la bandeja al
# iniciar sesión (HKLM Run: corre como cada usuario, sin permisos de administrador).
$ErrorActionPreference = 'Stop'

# Aether: red de seguridad. Este companion configura cosas de Aether durante la
# instalación; si algo inesperado falla, se registra un aviso y se continúa, nunca
# se aborta la instalación (regla de Aether: nada bloquea el acceso, solo avisa).
trap {
    if (Get-Command Write-AtlasLog -ErrorAction SilentlyContinue) {
        Write-AtlasLog -Level Warning -Message ("aether-centro: paso omitido por un error no crítico: " + $_.Exception.Message) -ErrorRecord $_
    }
    continue
}

$carpeta = Join-Path ([Environment]::GetFolderPath('Windows')) 'AtlasModules\Aether\Centro'
$centro = Join-Path $carpeta 'Centro-Aether.ps1'
$icono = Join-Path $carpeta 'aether.ico'
if (-not (Test-Path -LiteralPath $centro -PathType Leaf)) {
    throw "Falta el Centro Aether en '$centro'."
}
$powershell = Join-Path ([Environment]::SystemDirectory) 'WindowsPowerShell\v1.0\powershell.exe'
$wscript = Join-Path ([Environment]::SystemDirectory) 'wscript.exe'

# Aether: lanzador .vbs que abre el Centro SIN ninguna ventana de PowerShell (ventana
# oculta real, window style 0). Así no queda el proceso de PowerShell en la barra de tareas.
$vbs = Join-Path $carpeta 'Centro-Aether.vbs'
$vbsContenido = @"
' Lanza el Centro Aether sin ventana de PowerShell.
Dim shell, extra, i
Set shell = CreateObject("WScript.Shell")
extra = ""
For i = 0 To WScript.Arguments.Count - 1
    extra = extra & " " & WScript.Arguments(i)
Next
shell.Run "$powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File ""$centro""" & extra, 0, False
"@
[IO.File]::WriteAllText($vbs, $vbsContenido, (New-Object Text.UTF8Encoding($false)))

$shell = New-Object -ComObject WScript.Shell
foreach ($destino in @(
        (Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'Centro Aether.lnk'),
        (Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'Centro Aether.lnk')
    )) {
    try {
        $acceso = $shell.CreateShortcut($destino)
        $acceso.TargetPath = $wscript
        $acceso.Arguments = "`"$vbs`""
        $acceso.WorkingDirectory = $carpeta
        $acceso.IconLocation = "$icono,0"
        $acceso.Description = 'Centro Aether: modos, red, respaldo, limpieza y más.'
        $acceso.Save()
    }
    catch {
        Write-AtlasLog -Level Warning -Message "Centro Aether: no se pudo crear '$destino': $($_.Exception.Message)" -ErrorRecord $_
    }
}

$run = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'
if (-not (Test-Path -LiteralPath $run)) { New-Item -Path $run -Force | Out-Null }
Set-ItemProperty -LiteralPath $run -Name 'AetherBandeja' -Type String `
    -Value "`"$wscript`" `"$vbs`" -Bandeja"
Write-AtlasLog -Message 'Centro Aether listo (accesos directos y bandeja, sin ventana de consola).'
