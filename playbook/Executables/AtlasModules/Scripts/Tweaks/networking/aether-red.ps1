# Companion de aether-red.psd1. Versión automática (sin preguntas, sin reiniciar) de
# aether\red\Reparar-Red-Aether.ps1, el script que resolvió el error 0x80070035 en la
# laptop del usuario. Cada parte es de mejor esfuerzo: si una falla se registra un aviso
# y se sigue con las demás.
$ErrorActionPreference = 'Stop'

# Aether: red de seguridad. Este companion configura cosas de Aether durante la
# instalación; si algo inesperado falla, se registra un aviso y se continúa, nunca
# se aborta la instalación (regla de Aether: nada bloquea el acceso, solo avisa).
trap {
    if (Get-Command Write-AtlasLog -ErrorAction SilentlyContinue) {
        Write-AtlasLog -Level Warning -Message ("aether-red: paso omitido por un error no crítico: " + $_.Exception.Message) -ErrorRecord $_
    }
    continue
}

function Invoke-AetherRedPaso {
    param([string]$Nombre, [scriptblock]$Accion)
    try {
        & $Accion
    }
    catch {
        Write-AtlasLog -Level Warning -Message "Red Aether, '$Nombre' falló: $($_.Exception.Message)" -ErrorRecord $_
    }
}

$serviciosRoot = 'HKLM:\SYSTEM\CurrentControlSet\Services'

# 1. Drivers del cliente SMB con su inicio por defecto de Windows 11 si estaban desactivados.
Invoke-AetherRedPaso 'drivers SMB' {
    $drivers = [ordered]@{ 'rdbss' = 1; 'mrxsmb' = 3; 'mrxsmb20' = 3; 'bowser' = 3; 'NetBT' = 1 }
    foreach ($driver in $drivers.Keys) {
        $ruta = Join-Path $serviciosRoot $driver
        if (-not (Test-Path -LiteralPath $ruta)) { continue }
        $inicio = (Get-ItemProperty -LiteralPath $ruta -Name Start -ErrorAction SilentlyContinue).Start
        if ($inicio -eq 4) {
            Set-ItemProperty -LiteralPath $ruta -Name Start -Value $drivers[$driver] -Type DWord
            Write-AtlasLog -Message "Red Aether: driver $driver reactivado."
        }
    }
}

# 2. Estación de trabajo: dependencias que ya no existen (p. ej. MRxSmb10 tras quitar SMB1).
Invoke-AetherRedPaso 'dependencias de Estación de trabajo' {
    $rutaWks = Join-Path $serviciosRoot 'LanmanWorkstation'
    $dependencias = @((Get-ItemProperty -LiteralPath $rutaWks -Name DependOnService -ErrorAction SilentlyContinue).DependOnService)
    $faltantes = @($dependencias | Where-Object { $_ -and -not (Test-Path -LiteralPath (Join-Path $serviciosRoot $_)) })
    if ($faltantes.Count -gt 0) {
        Set-ItemProperty -LiteralPath $rutaWks -Name DependOnService -Value ([string[]]@('Bowser', 'MRxSmb20', 'NSI')) -Type MultiString
        Write-AtlasLog -Message "Red Aether: dependencias de LanmanWorkstation corregidas ($($faltantes -join ', '))."
    }
}

# 3. Servicios de red, carpetas compartidas e impresión.
Invoke-AetherRedPaso 'servicios' {
    $servicios = [ordered]@{
        'LanmanWorkstation' = 2   # Estación de trabajo: entrar a otras PCs
        'LanmanServer'      = 2   # Servidor: que otras entren a esta
        'lmhosts'           = 2   # Auxiliar NetBIOS sobre TCP/IP
        'fdPHost'           = 3   # Host de proveedor de detección de función
        'FDResPub'          = 2   # Publicación de recursos de detección
        'SSDPSRV'           = 3   # Detección SSDP
        'upnphost'          = 3   # Host de dispositivo UPnP
        'Spooler'           = 2   # Cola de impresión
        'PrintNotify'       = 3   # Extensiones y notificaciones de impresora
    }
    foreach ($nombre in $servicios.Keys) {
        $ruta = Join-Path $serviciosRoot $nombre
        if (-not (Test-Path -LiteralPath $ruta)) { continue }
        $inicio = (Get-ItemProperty -LiteralPath $ruta -Name Start -ErrorAction SilentlyContinue).Start
        if ($inicio -eq 4 -or ($servicios[$nombre] -eq 2 -and $inicio -ne 2)) {
            Set-ItemProperty -LiteralPath $ruta -Name Start -Value $servicios[$nombre] -Type DWord
            Write-AtlasLog -Message "Red Aether: servicio $nombre con inicio $($servicios[$nombre])."
        }
    }
}

# 4. LanmanWorkstation en el orden de proveedores de red (algunas VPN lo borran).
Invoke-AetherRedPaso 'proveedores de red' {
    foreach ($clave in @('Order', 'HwOrder')) {
        $ruta = "HKLM:\SYSTEM\CurrentControlSet\Control\NetworkProvider\$clave"
        if (-not (Test-Path -LiteralPath $ruta)) { continue }
        $actual = [string](Get-ItemProperty -LiteralPath $ruta -Name ProviderOrder -ErrorAction SilentlyContinue).ProviderOrder
        $lista = New-Object System.Collections.Generic.List[string]
        foreach ($p in @($actual -split ',' | Where-Object { $_ })) { $lista.Add($p) }
        if ($lista -contains 'LanmanWorkstation') { continue }
        $posicion = [Math]::Min(1, $lista.Count)
        if ($lista -contains 'RDPNP') { $posicion = $lista.IndexOf('RDPNP') + 1 }
        $lista.Insert($posicion, 'LanmanWorkstation')
        Set-ItemProperty -LiteralPath $ruta -Name ProviderOrder -Value ($lista -join ',') -Type String
        Write-AtlasLog -Message "Red Aether: LanmanWorkstation agregado a $clave."
    }
}

# 5. Enlaces de los adaptadores (cliente, servidor, IPv4, detección y LLDP) y NetBIOS.
Invoke-AetherRedPaso 'enlaces de adaptadores' {
    $componentes = @('ms_msclient', 'ms_server', 'ms_tcpip', 'ms_lltdio', 'ms_rspndr', 'ms_lldp')
    foreach ($enlace in @(Get-NetAdapterBinding -Name '*' -ErrorAction Stop |
                Where-Object { $componentes -ccontains [string]$_.ComponentID -and -not [bool]$_.Enabled })) {
        try {
            Enable-NetAdapterBinding -Name ([string]$enlace.Name) -ComponentID ([string]$enlace.ComponentID) `
                -Confirm:$false -ErrorAction Stop | Out-Null
        }
        catch {
            Write-AtlasLog -Level Warning -Message "Red Aether: no se pudo activar $($enlace.ComponentID) en '$($enlace.Name)': $($_.Exception.Message)" -ErrorRecord $_
        }
    }
}
Invoke-AetherRedPaso 'NetBIOS' {
    foreach ($interfaz in @(Get-ChildItem -LiteralPath "$serviciosRoot\NetBT\Parameters\Interfaces" -ErrorAction SilentlyContinue)) {
        Set-ItemProperty -LiteralPath $interfaz.PSPath -Name NetbiosOptions -Value 1 -Type DWord
    }
}

# 6. Redes en Privada y firewall de detección y uso compartido (grupos por ID: cualquier idioma).
Invoke-AetherRedPaso 'perfil de red' {
    foreach ($perfil in @(Get-NetConnectionProfile -ErrorAction SilentlyContinue)) {
        if ([string]$perfil.NetworkCategory -ceq 'Public') {
            Set-NetConnectionProfile -InterfaceIndex $perfil.InterfaceIndex -NetworkCategory Private -ErrorAction Stop
        }
    }
    Set-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\NcdAutoSetup\Private' `
        -Name AutoSetup -Value 1 -Type DWord -ErrorAction SilentlyContinue
}
Invoke-AetherRedPaso 'firewall' {
    foreach ($grupo in @('@FirewallAPI.dll,-32752', '@FirewallAPI.dll,-28502')) {
        $reglas = @(Get-NetFirewallRule -Group $grupo -ErrorAction SilentlyContinue |
                Where-Object { [string]$_.Profile -match 'Private|Domain|Any' -and [string]$_.Enabled -ne 'True' })
        if ($reglas.Count -gt 0) { $reglas | Enable-NetFirewallRule -ErrorAction Stop }
    }
    foreach ($regla in @(Get-NetFirewallRule -Direction Outbound -Action Block -Enabled True -ErrorAction SilentlyContinue)) {
        $puertos = @(($regla | Get-NetFirewallPortFilter -ErrorAction SilentlyContinue).RemotePort)
        if ($puertos -contains '445' -or $puertos -contains '139') {
            Disable-NetFirewallRule -Name $regla.Name -ErrorAction Stop
            Write-AtlasLog -Message "Red Aether: regla de firewall '$($regla.DisplayName)' bloqueaba carpetas compartidas; desactivada."
        }
    }
}

# 7. Seguridad SMB de Windows 11 24H2+: la causa real del 0x80070035 del usuario.
Invoke-AetherRedPaso 'seguridad SMB' {
    $rutaPolitica = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\LanmanWorkstation'
    if (-not (Test-Path -LiteralPath $rutaPolitica)) { New-Item -Path $rutaPolitica -Force | Out-Null }
    Set-ItemProperty -LiteralPath $rutaPolitica -Name AllowInsecureGuestAuth -Value 1 -Type DWord
    $parametros = "$serviciosRoot\LanmanWorkstation\Parameters"
    Set-ItemProperty -LiteralPath $parametros -Name RequireSecuritySignature -Value 0 -Type DWord
    Set-ItemProperty -LiteralPath $parametros -Name AllowInsecureGuestAuth -Value 1 -Type DWord
    try {
        Set-SmbClientConfiguration -RequireSecuritySignature $false -EnableInsecureGuestLogons $true -Force -ErrorAction Stop
    }
    catch {
        Write-AtlasLog -Level Warning -Message "Red Aether: Set-SmbClientConfiguration falló; quedan las claves del registro (toman efecto al reiniciar): $($_.Exception.Message)" -ErrorRecord $_
    }
}

# 8. Redes nuevas (Wi-Fi o cable) pasan a Privadas al conectarse, para compartir de una vez.
Invoke-AetherRedPaso 'tarea de redes privadas' {
    $script = Join-Path ([Environment]::GetFolderPath('Windows')) 'AtlasModules\Aether\Red\Redes-Privadas.ps1'
    if (-not (Test-Path -LiteralPath $script -PathType Leaf)) { throw "Falta '$script'." }
    $powershell = Join-Path ([Environment]::SystemDirectory) 'WindowsPowerShell\v1.0\powershell.exe'
    $accion = New-ScheduledTaskAction -Execute $powershell `
        -Argument "-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$script`""
    $clase = Get-CimClass -ClassName MSFT_TaskEventTrigger -Namespace 'Root/Microsoft/Windows/TaskScheduler'
    $alConectar = New-CimInstance -CimClass $clase -ClientOnly
    $alConectar.Enabled = $true
    $alConectar.Subscription = '<QueryList><Query Id="0" Path="Microsoft-Windows-NetworkProfile/Operational"><Select Path="Microsoft-Windows-NetworkProfile/Operational">*[System[(EventID=10000)]]</Select></Query></QueryList>'
    $disparadores = @($alConectar, (New-ScheduledTaskTrigger -AtStartup), (New-ScheduledTaskTrigger -AtLogOn))
    $principal = New-ScheduledTaskPrincipal -UserId 'S-1-5-18' -LogonType ServiceAccount -RunLevel Highest
    $ajustes = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
        -ExecutionTimeLimit (New-TimeSpan -Minutes 5) -MultipleInstances IgnoreNew
    Register-ScheduledTask -TaskName 'Aether Redes privadas' -TaskPath '\Aether\' -Action $accion `
        -Trigger $disparadores -Principal $principal -Settings $ajustes `
        -Description 'Aether OS: cambia a Privada cada red nueva para poder compartir carpetas e impresoras.' -Force | Out-Null
    Write-AtlasLog -Message 'Red Aether: tarea "Aether Redes privadas" registrada.'
}

# 9. "Uso compartido protegido con contraseña" DESACTIVADO (lo que hace el interruptor de
#    Configuración): cuenta Invitado activa y sin contraseña, sin el bloqueo de acceso por red,
#    "Todos" incluye a los anónimos y sin restricción de sesiones nulas. Así las PCs del trabajo
#    entran a las carpetas compartidas de esta sin pedir usuario ni contraseña.
Invoke-AetherRedPaso 'uso compartido sin contraseña' {
    $invitado = Get-LocalUser | Where-Object { $_.SID.Value -like 'S-1-5-*-501' } | Select-Object -First 1
    if ($invitado) {
        # Contraseña en blanco (net user acepta el nombre traducido: Invitado / Guest).
        & net.exe user $invitado.Name '""' | Out-Null
        Enable-LocalUser -SID $invitado.SID
        Write-AtlasLog -Message "Red Aether: cuenta '$($invitado.Name)' activa para el uso compartido sin contraseña."

        # Quitar a Invitado de "Denegar el acceso a este equipo desde la red" (SeDenyNetworkLogonRight).
        $temporal = Join-Path $env:TEMP ('aether-secpol-' + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $temporal -Force | Out-Null
        try {
            $inf = Join-Path $temporal 'actual.inf'
            & secedit.exe /export /cfg $inf /areas USER_RIGHTS /quiet | Out-Null
            $linea = Get-Content -LiteralPath $inf -Encoding Unicode | Where-Object { $_ -match '^SeDenyNetworkLogonRight\s*=' } | Select-Object -First 1
            if ($linea) {
                $sidInvitado = '*' + $invitado.SID.Value
                $miembros = @(($linea -split '=', 2)[1].Split(',') | ForEach-Object { $_.Trim() } |
                        Where-Object { $_ -and $_ -ne $sidInvitado -and $_ -ne $invitado.Name })
                $nuevo = Join-Path $temporal 'nuevo.inf'
                $db = Join-Path $temporal 'nuevo.sdb'
                @(
                    '[Unicode]', 'Unicode=yes',
                    '[Version]', 'signature="$CHICAGO$"', 'Revision=1',
                    '[Privilege Rights]', ('SeDenyNetworkLogonRight = ' + ($miembros -join ','))
                ) | Set-Content -LiteralPath $nuevo -Encoding Unicode
                & secedit.exe /configure /db $db /cfg $nuevo /areas USER_RIGHTS /quiet | Out-Null
                Write-AtlasLog -Message 'Red Aether: Invitado ya no tiene denegado el acceso por red.'
            }
        }
        finally { Remove-Item -LiteralPath $temporal -Recurse -Force -ErrorAction SilentlyContinue }
    }
    $lsa = 'HKLM:\SYSTEM\CurrentControlSet\Control\Lsa'
    Set-ItemProperty -LiteralPath $lsa -Name 'everyoneincludesanonymous' -Value 1 -Type DWord
    Set-ItemProperty -LiteralPath $lsa -Name 'forceguest' -Value 0 -Type DWord   # cuentas locales con su propio usuario
    Set-ItemProperty -LiteralPath (Join-Path $serviciosRoot 'LanmanServer\Parameters') -Name 'RestrictNullSessAccess' -Value 0 -Type DWord
    Write-AtlasLog -Message 'Red Aether: uso compartido protegido con contraseña desactivado.'
}

Write-AtlasLog -Message 'Red Aether: red de trabajo aplicada.'
