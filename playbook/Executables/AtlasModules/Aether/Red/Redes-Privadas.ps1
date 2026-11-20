<#
    Aether OS - Redes privadas.
    Lo corre la tarea "Aether Redes privadas" (como SYSTEM) cada vez que Windows conecta una
    red, al iniciar y al iniciar sesión: cambia a Privada toda red que Windows dejó como
    Pública, para que compartir carpetas e impresoras funcione desde el primer momento
    (las reglas del firewall para compartir están abiertas solo en redes privadas).
#>
$ErrorActionPreference = 'Continue'

# Windows tarda unos segundos en identificar la red recién conectada.
Start-Sleep -Seconds 8

foreach ($intento in 1..3) {
    $publicas = @(Get-NetConnectionProfile -ErrorAction SilentlyContinue | Where-Object { [string]$_.NetworkCategory -eq 'Public' })
    foreach ($perfil in $publicas) {
        try {
            Set-NetConnectionProfile -InterfaceIndex $perfil.InterfaceIndex -NetworkCategory Private -ErrorAction Stop
            Write-Output "Red '$($perfil.Name)' cambiada a Privada."
        }
        catch {
            Write-Output "No se pudo cambiar la red '$($perfil.Name)': $($_.Exception.Message)"
        }
    }
    if ($publicas.Count -eq 0) { break }
    Start-Sleep -Seconds 10
}
