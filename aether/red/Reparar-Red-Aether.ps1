<#
    Aether OS - Reparar red (lado cliente)
    Fuerza a que esta PC pueda ENTRAR a carpetas compartidas de otras PCs.

    Uso: doble clic en "Reparar-Red-Aether.cmd" (pide permisos de administrador).
    Antes de cambiar nada guarda un respaldo del registro en el Escritorio.
    Al final pide reiniciar la PC.
#>
[CmdletBinding()]
param(
    [string]$Equipo
)

$ErrorActionPreference = 'Continue'

# --- Elevación ---------------------------------------------------------------
$identidad = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $identidad.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $argumentos = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"")
    if ($Equipo) { $argumentos += @('-Equipo', "`"$Equipo`"") }
    Start-Process -FilePath 'powershell.exe' -ArgumentList $argumentos -Verb RunAs
    exit
}

$marca = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$escritorio = [Environment]::GetFolderPath('Desktop')
$carpetaRespaldo = Join-Path $escritorio "Aether-Red-Respaldo_$marca"
New-Item -ItemType Directory -Path $carpetaRespaldo -Force | Out-Null
Start-Transcript -Path (Join-Path $carpetaRespaldo 'registro.log') | Out-Null

function Titulo($texto) { Write-Host ''; Write-Host "=== $texto ===" -ForegroundColor Cyan }
function Ok($texto) { Write-Host "  [OK]      $texto" -ForegroundColor Green }
function Arreglado($texto) { Write-Host "  [ARREGLO] $texto" -ForegroundColor Yellow }
function Aviso($texto) { Write-Host "  [AVISO]   $texto" -ForegroundColor Magenta }
function Fallo($texto) { Write-Host "  [ERROR]   $texto" -ForegroundColor Red }

$cambios = New-Object System.Collections.Generic.List[string]

Write-Host ''
Write-Host '  AETHER OS - Reparar red (acceso a carpetas compartidas)' -ForegroundColor Cyan
Write-Host "  Respaldo y registro en: $carpetaRespaldo"

# --- 0. Respaldo -------------------------------------------------------------
Titulo 'Respaldo'
$clavesRespaldo = [ordered]@{
    'LanmanWorkstation'          = 'HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation'
    'NetworkProvider'            = 'HKLM\SYSTEM\CurrentControlSet\Control\NetworkProvider'
    'NetBT'                      = 'HKLM\SYSTEM\CurrentControlSet\Services\NetBT'
    'Politica-LanmanWorkstation' = 'HKLM\SOFTWARE\Policies\Microsoft\Windows\LanmanWorkstation'
}
foreach ($nombre in $clavesRespaldo.Keys) {
    $destino = Join-Path $carpetaRespaldo "$nombre.reg"
    & reg.exe export $clavesRespaldo[$nombre] $destino /y 2>$null | Out-Null
    if (Test-Path $destino) { Ok "Respaldado $nombre" }
}
try {
    Checkpoint-Computer -Description "Aether - antes de reparar red $marca" -RestorePointType MODIFY_SETTINGS -ErrorAction Stop
    Ok 'Punto de restauración creado'
} catch {
    Aviso 'No se pudo crear el punto de restauración (Protección del sistema apagada o ya se creó uno hoy). Se sigue con el respaldo .reg.'
}

# --- 1. Drivers del cliente SMB ------------------------------------------------
Titulo 'Drivers de red de Windows'
# Valor de inicio por defecto en Windows 11: 1 = sistema, 2 = automático, 3 = manual
$drivers = [ordered]@{ 'rdbss' = 1; 'mrxsmb' = 3; 'mrxsmb20' = 3; 'bowser' = 3; 'NetBT' = 1 }
foreach ($driver in $drivers.Keys) {
    $ruta = "HKLM:\SYSTEM\CurrentControlSet\Services\$driver"
    if (-not (Test-Path $ruta)) { Aviso "No existe el driver $driver"; continue }
    $inicio = (Get-ItemProperty -Path $ruta -Name Start -ErrorAction SilentlyContinue).Start
    if ($inicio -eq 4) {
        Set-ItemProperty -Path $ruta -Name Start -Value $drivers[$driver] -Type DWord
        Arreglado "$driver estaba DESACTIVADO; se reactivó"
        $cambios.Add("Driver $driver reactivado")
    } else {
        Ok "$driver activo"
    }
}

# --- 2. Dependencias del servicio Estación de trabajo -----------------------------
Titulo 'Servicio Estación de trabajo (cliente SMB)'
$rutaWks = 'HKLM:\SYSTEM\CurrentControlSet\Services\LanmanWorkstation'
$dependencias = @((Get-ItemProperty -Path $rutaWks -Name DependOnService -ErrorAction SilentlyContinue).DependOnService)
$faltantes = @($dependencias | Where-Object { $_ -and -not (Test-Path "HKLM:\SYSTEM\CurrentControlSet\Services\$_") })
if ($faltantes.Count -gt 0) {
    # Pasa cuando se quitó SMB1 a mano y quedó MRxSmb10 como dependencia: el servicio no arranca.
    & sc.exe config LanmanWorkstation depend= Bowser/MRxSmb20/NSI | Out-Null
    Arreglado "Dependía de componentes que ya no existen ($($faltantes -join ', ')); se corrigió"
    $cambios.Add('Dependencias de Estación de trabajo corregidas')
} else {
    Ok 'Dependencias correctas'
}

# --- 3. Servicios ---------------------------------------------------------------
Titulo 'Servicios de red'
$servicios = [ordered]@{
    'LanmanWorkstation' = 'Automatic'  # Estación de trabajo: entrar a otras PCs
    'LanmanServer'      = 'Automatic'  # Servidor: que otras entren a la tuya
    'lmhosts'           = 'Automatic'  # Aplicación auxiliar NetBIOS sobre TCP/IP
    'Dnscache'          = $null        # Cliente DNS (lo administra Windows)
    'NlaSvc'            = $null        # Reconocimiento de ubicación de red
    'netprofm'          = $null        # Servicio de lista de redes
    'fdPHost'           = 'Manual'     # Host de proveedor de detección de función
    'FDResPub'          = 'Automatic'  # Publicación de recursos de detección
    'SSDPSRV'           = 'Manual'     # Detección SSDP
    'upnphost'          = 'Manual'     # Host de dispositivo UPnP
}
foreach ($nombre in $servicios.Keys) {
    $svc = Get-Service -Name $nombre -ErrorAction SilentlyContinue
    if (-not $svc) { Aviso "No existe el servicio $nombre"; continue }
    $tipo = $servicios[$nombre]
    if ($tipo -and $svc.StartType -ne $tipo) {
        Set-Service -Name $nombre -StartupType $tipo -ErrorAction SilentlyContinue
        Arreglado "$($svc.DisplayName): inicio cambiado de $($svc.StartType) a $tipo"
        $cambios.Add("Servicio $nombre a $tipo")
    }
    if ((Get-Service -Name $nombre).Status -ne 'Running') {
        try {
            Start-Service -Name $nombre -ErrorAction Stop
            Arreglado "$($svc.DisplayName): iniciado"
        } catch {
            if ($nombre -eq 'LanmanWorkstation') { Fallo "$($svc.DisplayName) no arranca: $($_.Exception.Message) (debería arrancar tras reiniciar)" }
            else { Aviso "$($svc.DisplayName) no se pudo iniciar ahora; arrancará cuando Windows lo necesite" }
        }
    } else {
        Ok "$($svc.DisplayName) en ejecución"
    }
}

# --- 4. Orden de proveedores de red ----------------------------------------------
Titulo 'Proveedor de red de Microsoft'
foreach ($clave in @('Order', 'HwOrder')) {
    $ruta = "HKLM:\SYSTEM\CurrentControlSet\Control\NetworkProvider\$clave"
    if (-not (Test-Path $ruta)) { continue }
    $actual = [string](Get-ItemProperty -Path $ruta -Name ProviderOrder -ErrorAction SilentlyContinue).ProviderOrder
    $lista = @($actual -split ',' | Where-Object { $_ })
    if ($lista -notcontains 'LanmanWorkstation') {
        # Algunas VPN o programas de red lo borran y Windows deja de abrir \\PC (error 0x80070035).
        $nueva = New-Object System.Collections.Generic.List[string]
        foreach ($p in $lista) { $nueva.Add($p) }
        $posicion = [Math]::Min(1, $nueva.Count)
        if ($nueva -contains 'RDPNP') { $posicion = $nueva.IndexOf('RDPNP') + 1 }
        $nueva.Insert($posicion, 'LanmanWorkstation')
        Set-ItemProperty -Path $ruta -Name ProviderOrder -Value ($nueva -join ',') -Type String
        Arreglado "Faltaba LanmanWorkstation en $clave (ahora: $($nueva -join ','))"
        $cambios.Add("LanmanWorkstation agregado a $clave")
    } else {
        Ok "$clave correcto ($actual)"
    }
}

# --- 5. Enlaces de los adaptadores -------------------------------------------------
Titulo 'Adaptadores de red'
$adaptadores = @(Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object Status -eq 'Up')
if ($adaptadores.Count -eq 0) { Aviso 'No hay adaptadores físicos conectados' }
foreach ($adaptador in $adaptadores) {
    foreach ($componente in @(
            @{ Id = 'ms_msclient'; Nombre = 'Cliente para redes Microsoft' },
            @{ Id = 'ms_server'; Nombre = 'Uso compartido de archivos e impresoras' },
            @{ Id = 'ms_tcpip'; Nombre = 'Protocolo de Internet versión 4' })) {
        $enlace = Get-NetAdapterBinding -Name $adaptador.Name -ComponentID $componente.Id -ErrorAction SilentlyContinue
        if (-not $enlace) { continue }
        if (-not $enlace.Enabled) {
            Enable-NetAdapterBinding -Name $adaptador.Name -ComponentID $componente.Id
            Arreglado "$($adaptador.Name): se activó '$($componente.Nombre)'"
            $cambios.Add("$($componente.Id) activado en $($adaptador.Name)")
        } else {
            Ok "$($adaptador.Name): '$($componente.Nombre)' activo"
        }
    }
}

# NetBIOS sobre TCP/IP forzado en todas las interfaces (1 = activado)
$interfaces = Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces' -ErrorAction SilentlyContinue
$netbiosCambiado = $false
foreach ($interfaz in $interfaces) {
    $valor = (Get-ItemProperty -Path $interfaz.PSPath -Name NetbiosOptions -ErrorAction SilentlyContinue).NetbiosOptions
    if ($valor -ne 1) {
        Set-ItemProperty -Path $interfaz.PSPath -Name NetbiosOptions -Value 1 -Type DWord
        $netbiosCambiado = $true
    }
}
if ($netbiosCambiado) { Arreglado 'NetBIOS sobre TCP/IP forzado a ACTIVADO'; $cambios.Add('NetBIOS activado') }
else { Ok 'NetBIOS sobre TCP/IP activado' }

# --- 6. Perfil de red y firewall ---------------------------------------------------
Titulo 'Perfil de red y firewall'
foreach ($perfil in @(Get-NetConnectionProfile -ErrorAction SilentlyContinue)) {
    if ($perfil.NetworkCategory -eq 'Public') {
        Set-NetConnectionProfile -InterfaceIndex $perfil.InterfaceIndex -NetworkCategory Private
        Arreglado "Red '$($perfil.Name)' cambiada de Pública a Privada"
        $cambios.Add("Red $($perfil.Name) a Privada")
    } else {
        Ok "Red '$($perfil.Name)': $($perfil.NetworkCategory)"
    }
}

# Grupos por su ID interno: funcionan con Windows en cualquier idioma
$grupos = @(
    @{ Id = '@FirewallAPI.dll,-32752'; Nombre = 'Detección de redes' },
    @{ Id = '@FirewallAPI.dll,-28502'; Nombre = 'Compartir archivos e impresoras' }
)
foreach ($grupo in $grupos) {
    $reglas = @(Get-NetFirewallRule -Group $grupo.Id -ErrorAction SilentlyContinue |
        Where-Object { $_.Profile -match 'Private|Domain|Any' -and $_.Enabled -ne 'True' })
    if ($reglas.Count -gt 0) {
        $reglas | Enable-NetFirewallRule
        Arreglado "Firewall: se activaron $($reglas.Count) reglas de '$($grupo.Nombre)'"
        $cambios.Add("Firewall $($grupo.Nombre)")
    } else {
        Ok "Firewall: '$($grupo.Nombre)' permitido"
    }
}

# Reglas de salida que bloqueen el puerto de carpetas compartidas (445/139)
$bloqueos = @(Get-NetFirewallRule -Direction Outbound -Action Block -Enabled True -ErrorAction SilentlyContinue |
    Where-Object { $f = $_ | Get-NetFirewallPortFilter; @($f.RemotePort) -contains '445' -or @($f.RemotePort) -contains '139' })
foreach ($regla in $bloqueos) {
    Disable-NetFirewallRule -Name $regla.Name
    Arreglado "Firewall: se desactivó la regla que bloqueaba la salida a carpetas compartidas: '$($regla.DisplayName)'"
    $cambios.Add("Regla de bloqueo $($regla.DisplayName) desactivada")
}
if ($bloqueos.Count -eq 0) { Ok 'Ninguna regla bloquea la salida a carpetas compartidas' }

# --- 7. Seguridad SMB de Windows 11 24H2+ (causa más probable) ------------------------
Titulo 'Compatibilidad con carpetas compartidas de otras PCs'
# Desde 24H2, Windows 11 exige firma SMB y bloquea el acceso "invitado" (carpetas
# compartidas sin contraseña). Las PCs más viejas entran a la tuya, pero la tuya
# no entra a las de ellas: justo tu síntoma.
$cliente = Get-SmbClientConfiguration
if ($cliente.RequireSecuritySignature) {
    Set-SmbClientConfiguration -RequireSecuritySignature $false -Force
    Arreglado 'Firma SMB obligatoria DESACTIVADA (sigue firmando cuando la otra PC lo admite)'
    $cambios.Add('RequireSecuritySignature = false')
} else {
    Ok 'Firma SMB no obligatoria'
}
if (-not $cliente.EnableInsecureGuestLogons) {
    Set-SmbClientConfiguration -EnableInsecureGuestLogons $true -Force
    Arreglado 'Acceso a carpetas compartidas sin contraseña (invitado) PERMITIDO'
    $cambios.Add('EnableInsecureGuestLogons = true')
} else {
    Ok 'Acceso invitado permitido'
}
$rutaPolitica = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\LanmanWorkstation'
if (-not (Test-Path $rutaPolitica)) { New-Item -Path $rutaPolitica -Force | Out-Null }
Set-ItemProperty -Path $rutaPolitica -Name AllowInsecureGuestAuth -Value 1 -Type DWord
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters' -Name RequireSecuritySignature -Value 0 -Type DWord
Ok 'Directivas de invitado y firma fijadas (no se revierten solas)'

# --- 8. Limpiar cachés ------------------------------------------------------------
Titulo 'Limpieza de cachés de red'
& ipconfig.exe /flushdns | Out-Null; Ok 'Caché DNS limpia'
& nbtstat.exe -R | Out-Null; Ok 'Caché de nombres NetBIOS limpia'
& nbtstat.exe -RR | Out-Null; Ok 'Nombres NetBIOS re-registrados'
& arp.exe -d '*' 2>$null | Out-Null; Ok 'Caché ARP limpia'
& klist.exe purge 2>$null | Out-Null

# --- 9. Prueba con un equipo ------------------------------------------------------
Titulo 'Prueba'
if (-not $Equipo) {
    $Equipo = Read-Host '  Escribe el nombre o la IP de una PC a la que quieras entrar (ej. PC10). Enter para omitir'
}
$Equipo = ($Equipo -replace '^\\\\', '').Trim()
if ($Equipo) {
    $ip = $null
    try {
        $ip = ([System.Net.Dns]::GetHostAddresses($Equipo) | Where-Object AddressFamily -eq 'InterNetwork' | Select-Object -First 1).IPAddressToString
    } catch { }
    if ($ip) { Ok "'$Equipo' se encontró en $ip" }
    else { Fallo "No se pudo traducir '$Equipo' a una IP. Prueba con la IP directa (en esa PC: ipconfig)." }

    $destino = if ($ip) { $ip } else { $Equipo }
    $p445 = Test-NetConnection -ComputerName $destino -Port 445 -WarningAction SilentlyContinue -InformationLevel Quiet
    $p139 = Test-NetConnection -ComputerName $destino -Port 139 -WarningAction SilentlyContinue -InformationLevel Quiet
    if ($p445) { Ok "Puerto 445 (carpetas compartidas) abierto en $destino" }
    elseif ($p139) { Aviso "Solo responde el puerto 139: ese equipo podría usar SMB1 (protocolo antiguo)" }
    else { Fallo "$destino no responde en 445 ni en 139. Si funcionaba antes, reinicia y vuelve a probar." }

    if ((Get-Service LanmanWorkstation).Status -eq 'Running') {
        Write-Host ''
        Write-Host "  Carpetas compartidas de \\$destino :" -ForegroundColor Cyan
        & net.exe view "\\$destino" 2>&1 | ForEach-Object { Write-Host "    $_" }
    }
} else {
    Aviso 'Prueba omitida'
}

# --- Resumen -----------------------------------------------------------------------
Titulo 'Resumen'
if ($cambios.Count -eq 0) {
    Ok 'No hizo falta cambiar nada en esta PC.'
} else {
    Write-Host "  Se hicieron $($cambios.Count) cambios:" -ForegroundColor Yellow
    $cambios | ForEach-Object { Write-Host "   - $_" }
}
Write-Host ''
Write-Host '  Para deshacer: doble clic en los archivos .reg de la carpeta de respaldo, o usa el punto de restauración.' -ForegroundColor Gray
Write-Host "  Carpeta: $carpetaRespaldo" -ForegroundColor Gray
Stop-Transcript | Out-Null

Write-Host ''
$respuesta = Read-Host '  Hay que REINICIAR para que todo tome efecto. ¿Reiniciar ahora? (S/N)'
if ($respuesta -match '^[sS]') { Restart-Computer -Force }
