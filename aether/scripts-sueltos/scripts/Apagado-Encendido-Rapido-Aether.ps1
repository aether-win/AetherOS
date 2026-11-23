<#
    Aether OS - Apagado, reinicio y encendido más rápidos
    Acorta las esperas al apagar/reiniciar, quita el retraso de las apps de inicio y activa el
    inicio rápido de Windows. No fuerza el cierre de programas con trabajo sin guardar.
#>
# Pide permisos de administrador si no los tiene.
$yo = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $yo.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"")
    exit
}
$ErrorActionPreference = 'Continue'
function Poner { param($Ruta, $Nombre, $Valor, $Tipo)
    if (-not (Test-Path -LiteralPath $Ruta)) { New-Item -Path $Ruta -Force | Out-Null }
    Set-ItemProperty -LiteralPath $Ruta -Name $Nombre -Value $Valor -Type $Tipo
    Write-Host "  - $Nombre = $Valor"
}
Poner 'HKLM:\SYSTEM\CurrentControlSet\Control' 'WaitToKillServiceTimeout' '2000' String
Poner 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management' 'ClearPageFileAtShutdown' 0 DWord
Poner 'HKCU:\Control Panel\Desktop' 'WaitToKillAppTimeout' '4000' String
Poner 'HKCU:\Control Panel\Desktop' 'HungAppTimeout' '4000' String
Poner 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize' 'StartupDelayInMSec' 0 DWord
# Inicio rápido de Windows (necesita la hibernación activa).
& powercfg.exe /hibernate on 2>&1 | Out-Null
Poner 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power' 'HiberbootEnabled' 1 DWord
Write-Host ''
Write-Host 'Listo: apagado, reinicio y encendido más rápidos.' -ForegroundColor Green
