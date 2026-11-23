<#
    Aether OS - Bluetooth sólido (mandos, audífonos, ratón y teclado)
#>
# Pide permisos de administrador si no los tiene.
$yo = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $yo.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"")
    exit
}
# Companion de aether-bluetooth.psd1: Bluetooth sólido (mandos, audífonos, ratón y teclado).
# Todo es de mejor esfuerzo y se puede repetir: el Guardián de Aether lo vuelve a aplicar al
# arrancar, porque Windows rehace los ajustes de energía de un dispositivo al reinstalarlo.
$ErrorActionPreference = 'Stop'
trap {
    if (Get-Command Write-AtlasLog -ErrorAction SilentlyContinue) {
        Write-AtlasLog -Level Warning -Message ('aether-bluetooth: paso omitido: ' + $_.Exception.Message) -ErrorRecord $_
    }
    continue
}
if (-not (Get-Command Write-AtlasLog -ErrorAction SilentlyContinue)) {
    function Write-AtlasLog { param([string]$Message, [string]$Level = 'Info', $ErrorRecord) if ($Level -eq 'Warning') { Write-Host "  ! $Message" -ForegroundColor Yellow } else { Write-Host "  - $Message" } }
}
function Invoke-AetherBtPaso {
    param([string]$Nombre, [scriptblock]$Accion)
    try { & $Accion }
    catch { Write-AtlasLog -Level Warning -Message "Bluetooth Aether, '$Nombre' falló: $($_.Exception.Message)" -ErrorRecord $_ }
}

# 1. Servicios de Bluetooth siempre disponibles (no "a petición"): el radio y los mandos/audio
#    vuelven al instante tras suspender o reiniciar.
Invoke-AetherBtPaso 'servicios' {
    foreach ($s in @(
            @{ Nombre = 'bthserv'; Inicio = 2 },          # Servicio de compatibilidad con Bluetooth
            @{ Nombre = 'BthAvctpSvc'; Inicio = 2 },      # Control de audio/vídeo (AVCTP)
            @{ Nombre = 'BTAGService'; Inicio = 3 },      # Puerta de enlace de audio (manual, como Windows)
            @{ Nombre = 'DeviceAssociationService'; Inicio = 3 }
        )) {
        $clave = "HKLM:\SYSTEM\CurrentControlSet\Services\$($s.Nombre)"
        if (-not (Test-Path -LiteralPath $clave)) { continue }
        Set-ItemProperty -LiteralPath $clave -Name Start -Value $s.Inicio -Type DWord
        if ($s.Inicio -eq 2) { Start-Service -Name $s.Nombre -ErrorAction SilentlyContinue }
    }
    # Servicio por usuario (BluetoothUserService_xxxxx): plantilla en manual (valor de Windows).
    $plantilla = 'HKLM:\SYSTEM\CurrentControlSet\Services\BluetoothUserService'
    if (Test-Path -LiteralPath $plantilla) { Set-ItemProperty -LiteralPath $plantilla -Name Start -Value 3 -Type DWord }
}

# 2. Que Windows nunca apague el radio Bluetooth ni los dispositivos Bluetooth para ahorrar
#    energía (la casilla "Permitir que el equipo apague este dispositivo"): causa típica de
#    mandos que se desconectan solos y audio que se corta.
Invoke-AetherBtPaso 'ahorro de energía de dispositivos' {
    $dispositivos = @(Get-PnpDevice -PresentOnly -ErrorAction SilentlyContinue | Where-Object {
            $_.Class -eq 'Bluetooth' -or $_.InstanceId -like 'BTH*' -or $_.InstanceId -like 'BTHENUM*' -or
            ($_.Class -eq 'HIDClass' -and $_.InstanceId -match '^(BTHENUM|BTHLEDEVICE|HID\\\{00001124)')
        })
    $energia = @(Get-CimInstance -Namespace root\wmi -ClassName MSPower_DeviceEnable -ErrorAction SilentlyContinue)
    $n = 0
    foreach ($d in $dispositivos) {
        # Registro del dispositivo: sin suspensión selectiva ni gestión de energía mejorada.
        $parametros = "HKLM:\SYSTEM\CurrentControlSet\Enum\$($d.InstanceId)\Device Parameters"
        if (Test-Path -LiteralPath $parametros) {
            foreach ($valor in 'SelectiveSuspendEnabled', 'EnhancedPowerManagementEnabled', 'AllowIdleIrpInD3', 'DeviceSelectiveSuspended') {
                Set-ItemProperty -LiteralPath $parametros -Name $valor -Value 0 -Type DWord -ErrorAction SilentlyContinue
            }
        }
        foreach ($e in $energia) {
            if ($e.InstanceName -like "$($d.InstanceId)*" -and $e.Enable) {
                Set-CimInstance -InputObject $e -Property @{ Enable = $false } -ErrorAction SilentlyContinue
                $n++
            }
        }
    }
    Write-AtlasLog -Message "Bluetooth Aether: ahorro de energía quitado en $n dispositivos ($($dispositivos.Count) revisados)."
}

# 3. El radio Bluetooth suele ir por USB interno: su concentrador USB tampoco se duerme.
Invoke-AetherBtPaso 'concentrador USB del radio' {
    $radios = @(Get-PnpDevice -Class Bluetooth -PresentOnly -ErrorAction SilentlyContinue | Where-Object { $_.InstanceId -like 'USB\*' })
    foreach ($r in $radios) {
        $padre = (Get-PnpDeviceProperty -InstanceId $r.InstanceId -KeyName 'DEVPKEY_Device_Parent' -ErrorAction SilentlyContinue).Data
        if (-not $padre) { continue }
        $parametros = "HKLM:\SYSTEM\CurrentControlSet\Enum\$padre\Device Parameters"
        if (Test-Path -LiteralPath $parametros) {
            Set-ItemProperty -LiteralPath $parametros -Name 'SelectiveSuspendEnabled' -Value 0 -Type DWord -ErrorAction SilentlyContinue
        }
        Get-CimInstance -Namespace root\wmi -ClassName MSPower_DeviceEnable -ErrorAction SilentlyContinue |
            Where-Object { $_.InstanceName -like "$padre*" -and $_.Enable } |
            ForEach-Object { Set-CimInstance -InputObject $_ -Property @{ Enable = $false } -ErrorAction SilentlyContinue }
    }
}

# 4. Pila Bluetooth: sin ahorro agresivo en el controlador USB de Bluetooth y sin que el
#    radio se apague en modo avión al volver de suspensión.
Invoke-AetherBtPaso 'pila Bluetooth' {
    $bthusb = 'HKLM:\SYSTEM\CurrentControlSet\Services\BTHUSB\Parameters'
    if (-not (Test-Path -LiteralPath $bthusb)) { New-Item -Path $bthusb -Force | Out-Null }
    Set-ItemProperty -LiteralPath $bthusb -Name 'SelectiveSuspendEnabled' -Value 0 -Type DWord
    # Coexistencia Wi-Fi/Bluetooth (adaptadores Intel/Realtek/MediaTek que lo exponen): activada.
    foreach ($a in @(Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object { $_.MediaType -eq 'Native 802.11' -or $_.PhysicalMediaType -match '802.11' })) {
        foreach ($p in @(Get-NetAdapterAdvancedProperty -Name $a.Name -ErrorAction SilentlyContinue | Where-Object { $_.DisplayName -match '(?i)bluetooth' })) {
            $activado = @($p.ValidDisplayValues | Where-Object { $_ -match '(?i)^(enabled|habilitado|activado|on)' }) | Select-Object -First 1
            if ($activado -and $p.DisplayValue -ne $activado) {
                Set-NetAdapterAdvancedProperty -Name $a.Name -DisplayName $p.DisplayName -DisplayValue $activado -NoRestart -ErrorAction SilentlyContinue
                Write-AtlasLog -Message "Bluetooth Aether: '$($p.DisplayName)' activado en $($a.Name)."
            }
        }
    }
}
Write-AtlasLog -Message 'Bluetooth Aether aplicado.'

Write-Host ''
Write-Host 'Bluetooth sólido aplicado. Si un mando seguía fallando, apágalo y vuelve a conectarlo.' -ForegroundColor Green
