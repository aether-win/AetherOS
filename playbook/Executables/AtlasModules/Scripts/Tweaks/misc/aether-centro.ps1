# Companion de aether-centro.psd1: accesos directos del Centro Aether y la bandeja al
# iniciar sesión (HKLM Run: corre como cada usuario, sin permisos de administrador).
$ErrorActionPreference = 'Stop'

$carpeta = Join-Path ([Environment]::GetFolderPath('Windows')) 'AtlasModules\Aether\Centro'
$centro = Join-Path $carpeta 'Centro-Aether.ps1'
$icono = Join-Path $carpeta 'aether.ico'
if (-not (Test-Path -LiteralPath $centro -PathType Leaf)) {
    throw "Falta el Centro Aether en '$centro'."
}
$powershell = Join-Path ([Environment]::SystemDirectory) 'WindowsPowerShell\v1.0\powershell.exe'

$shell = New-Object -ComObject WScript.Shell
foreach ($destino in @(
        (Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'Centro Aether.lnk'),
        (Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'Centro Aether.lnk')
    )) {
    try {
        $acceso = $shell.CreateShortcut($destino)
        $acceso.TargetPath = $powershell
        $acceso.Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$centro`""
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
    -Value "`"$powershell`" -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$centro`" -Bandeja"
Write-AtlasLog -Message 'Centro Aether listo (accesos directos y bandeja).'
