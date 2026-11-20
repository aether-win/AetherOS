<#
    Aether OS - Funciones del Centro Aether.
    Las usan la ventana (Centro-Aether.ps1), el proceso de la bandeja (-Bandeja) y el respaldo.
    Todo corre como el usuario, sin permisos de administrador, salvo lo que se abre con
    Start-AetherComoAdmin (pide UAC).
#>
Set-StrictMode -Version 2.0

$script:AetherCarpeta = Split-Path -Parent $PSScriptRoot
$script:AetherClave = 'HKCU:\Software\AetherOS\Centro'
$script:AetherEscritorio = Join-Path ([Environment]::GetFolderPath('Windows')) 'AtlasDesktop'
$script:AetherDatos = Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'Aether'

# --- Ajustes guardados (HKCU) --------------------------------------------------------
function Get-AetherAjuste {
    param([Parameter(Mandatory = $true)][string]$Nombre, $PorDefecto = $null)
    try {
        $valor = Get-ItemPropertyValue -LiteralPath $script:AetherClave -Name $Nombre -ErrorAction Stop
        return $valor
    }
    catch {
        return $PorDefecto
    }
}

function Set-AetherAjuste {
    param([Parameter(Mandatory = $true)][string]$Nombre, [Parameter(Mandatory = $true)]$Valor)
    if (-not (Test-Path -LiteralPath $script:AetherClave)) {
        New-Item -Path $script:AetherClave -Force | Out-Null
    }
    $tipo = if ($Valor -is [int] -or $Valor -is [bool]) { 'DWord' } else { 'String' }
    if ($Valor -is [bool]) { $Valor = [int]$Valor }
    Set-ItemProperty -LiteralPath $script:AetherClave -Name $Nombre -Value $Valor -Type $tipo
}

# --- Energía ---------------------------------------------------------------------------
$script:AetherSubProcesador = '54533251-82be-4824-96c1-47b60b740d00'
$script:AetherTurbo = 'be337238-0d82-4146-a960-4f3749d470c7'        # PERFBOOSTMODE
$script:AetherMinNucleos = '0cc5b647-c1df-4637-891a-dec35c318583'   # CPMINCORES (estacionar núcleos)
$script:AetherEpp = '36687f9e-e3a5-4dbf-b1dc-15eb381c6863'          # PERFEPP (0 = rendimiento)
$script:AetherProcMin = '893dee8e-2bef-41e0-89c6-b55d0929964c'      # PROCTHROTTLEMIN (% mínimo CPU)
$script:AetherProcMax = 'bc5038f7-23e0-4960-96da-33abaf5935ec'      # PROCTHROTTLEMAX (% máximo CPU)
$script:AetherEnfriamiento = '94d3a615-a899-4ac5-ae2b-e4d8f634367f' # SYSCOOLPOL (1 = activo)
$script:AetherSubAhorro = 'de830923-a562-41af-a086-e3a2c6bad2da'
$script:AetherUmbralAhorro = 'e69653ca-cf7f-4f05-aa73-cb833fa90ad4' # ESBATTTHRESHOLD
# Plantilla "Rendimiento máximo" (Ultimate Performance) de Windows. Aether la desbloquea
# y la duplica para crear su propio paquete de energía, sin tocar los planes del usuario.
$script:AetherPlantillaMax = 'e9a42b02-d5df-448d-aa00-03f14749eb61'
$script:AetherAjustesEnergia = @(
    @{ Clave = 'Turbo'; Sub = $script:AetherSubProcesador; Ajuste = $script:AetherTurbo }
    @{ Clave = 'MinNucleos'; Sub = $script:AetherSubProcesador; Ajuste = $script:AetherMinNucleos }
    @{ Clave = 'Epp'; Sub = $script:AetherSubProcesador; Ajuste = $script:AetherEpp }
    @{ Clave = 'UmbralAhorro'; Sub = $script:AetherSubAhorro; Ajuste = $script:AetherUmbralAhorro }
)

if (-not ('Aether.Nativo' -as [type])) {
    Add-Type -Namespace Aether -Name Nativo -MemberDefinition @'
[DllImport("powrprof.dll")]
public static extern uint PowerSetActiveOverlayScheme(Guid overlaySchemeGuid);
[DllImport("ntdll.dll")]
public static extern int NtSetTimerResolution(uint desiredResolution, bool setResolution, out uint currentResolution);
'@
}

# Modo de energía de Windows (Configuración > Sistema > Energía): mejor rendimiento,
# equilibrado o mayor eficiencia.
$script:AetherSuperposicion = @{
    Rendimiento = [Guid]'ded574b5-45a0-4f42-8737-46345c09c238'
    Equilibrado = [Guid]'00000000-0000-0000-0000-000000000000'
    Eficiencia  = [Guid]'961cc777-2547-4f9d-8174-7d86181b8a7a'
}

function Set-AetherModoEnergiaWindows {
    param([ValidateSet('Rendimiento', 'Equilibrado', 'Eficiencia')][string]$Modo)
    try {
        [void][Aether.Nativo]::PowerSetActiveOverlayScheme($script:AetherSuperposicion[$Modo])
    }
    catch {
        Write-Warning "No se pudo cambiar el modo de energía de Windows: $($_.Exception.Message)"
    }
}

function Get-AetherValorEnergia {
    # Devuelve @(AC, DC). powercfg escribe la salida en el idioma de Windows; los dos
    # últimos valores "0x..." de la consulta son siempre los índices actuales AC y DC.
    param([string]$Sub, [string]$Ajuste)
    $salida = @(& powercfg.exe /query SCHEME_CURRENT $Sub $Ajuste 2>$null)
    $valores = @($salida | ForEach-Object { [regex]::Matches([string]$_, '0x[0-9a-fA-F]+') } | ForEach-Object { $_.Value })
    if ($valores.Count -lt 2) { return $null }
    return @([Convert]::ToInt32($valores[-2], 16), [Convert]::ToInt32($valores[-1], 16))
}

function Set-AetherValorEnergia {
    param([string]$Sub, [string]$Ajuste, $Ac = $null, $Dc = $null)
    if ($null -ne $Ac) { & powercfg.exe /setacvalueindex SCHEME_CURRENT $Sub $Ajuste ([int]$Ac) 2>&1 | Out-Null }
    if ($null -ne $Dc) { & powercfg.exe /setdcvalueindex SCHEME_CURRENT $Sub $Ajuste ([int]$Dc) 2>&1 | Out-Null }
}

function Save-AetherEnergiaOriginal {
    # La primera vez guarda los valores del plan de energía para que Trabajo los ponga de vuelta.
    if ((Get-AetherAjuste -Nombre 'OriginalGuardado' -PorDefecto 0) -eq 1) { return }
    foreach ($ajuste in $script:AetherAjustesEnergia) {
        $valores = Get-AetherValorEnergia -Sub $ajuste.Sub -Ajuste $ajuste.Ajuste
        if ($null -ne $valores) {
            Set-AetherAjuste -Nombre "Original$($ajuste.Clave)" -Valor "$($valores[0]),$($valores[1])"
        }
    }
    Set-AetherAjuste -Nombre 'OriginalGuardado' -Valor 1
}

function Restore-AetherEnergiaOriginal {
    foreach ($ajuste in $script:AetherAjustesEnergia) {
        $guardado = [string](Get-AetherAjuste -Nombre "Original$($ajuste.Clave)" -PorDefecto '')
        if ($guardado -match '^(\d+),(\d+)$') {
            Set-AetherValorEnergia -Sub $ajuste.Sub -Ajuste $ajuste.Ajuste -Ac $Matches[1] -Dc $Matches[2]
        }
    }
}

function Initialize-AetherPlanEnergia {
    <#
        Crea (una sola vez) el paquete de energía propio de Aether duplicando la plantilla
        "Rendimiento máximo" de Windows, lo deja activo y guarda su GUID. No modifica los
        planes de energía que el usuario ya tenga. Idempotente: si el plan ya existe, solo
        lo reactiva. Devuelve el GUID del plan de Aether, o $null si no se pudo crear.
    #>
    $guid = [string](Get-AetherAjuste -Nombre 'PlanEnergiaGuid' -PorDefecto '')
    $lista = @(& powercfg.exe /list 2>$null)
    $existe = $guid -and ($lista -match [regex]::Escape($guid))
    if (-not $existe) {
        # Desbloquea la plantilla de Rendimiento máximo (si Windows la tiene oculta) y la duplica.
        $salida = & powercfg.exe -duplicatescheme $script:AetherPlantillaMax 2>$null
        $nuevo = ([regex]::Match(($salida -join ' '), '[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}')).Value
        if (-not $nuevo) {
            # Algunos equipos no traen la plantilla: se duplica el plan activo como respaldo.
            $salida = & powercfg.exe -duplicatescheme SCHEME_CURRENT 2>$null
            $nuevo = ([regex]::Match(($salida -join ' '), '[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}')).Value
        }
        if (-not $nuevo) { return $null }
        $guid = $nuevo
        & powercfg.exe -changename $guid 'Aether' 'Paquete de energía de Aether (Gaming / Trabajo / Batería).' 2>&1 | Out-Null
        Set-AetherAjuste -Nombre 'PlanEnergiaGuid' -Valor $guid
    }
    & powercfg.exe /setactive $guid 2>&1 | Out-Null
    return $guid
}

function Test-AetherCargador {
    Add-Type -AssemblyName System.Windows.Forms
    return [System.Windows.Forms.SystemInformation]::PowerStatus.PowerLineStatus -eq 'Online'
}

function Get-AetherBateria {
    # Porcentaje de batería, o $null si la PC no tiene batería.
    Add-Type -AssemblyName System.Windows.Forms
    $estado = [System.Windows.Forms.SystemInformation]::PowerStatus
    if ($estado.BatteryChargeStatus -band [System.Windows.Forms.BatteryChargeStatus]::NoSystemBattery) { return $null }
    return [int][Math]::Round($estado.BatteryLifePercent * 100)
}

function Set-AetherNotificaciones {
    param([bool]$Activas)
    $ruta = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings'
    if (-not (Test-Path -LiteralPath $ruta)) { New-Item -Path $ruta -Force | Out-Null }
    Set-ItemProperty -LiteralPath $ruta -Name 'NOC_GLOBAL_SETTING_TOASTS_ENABLED' -Value ([int]$Activas) -Type DWord
}

function Set-AetherAppsSegundoPlano {
    param([bool]$Permitidas)
    $ruta = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications'
    if (-not (Test-Path -LiteralPath $ruta)) { New-Item -Path $ruta -Force | Out-Null }
    Set-ItemProperty -LiteralPath $ruta -Name 'GlobalUserDisabled' -Value ([int](-not $Permitidas)) -Type DWord
}

# --- Wind-Aether (Windhawk) ------------------------------------------------------------
function Get-AetherWindhawkCli {
    foreach ($base in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) {
        if (-not $base) { continue }
        $ruta = Join-Path $base 'Windhawk\windhawk-cli.exe'
        if (Test-Path -LiteralPath $ruta) { return $ruta }
    }
    return $null
}

function Get-AetherPerfilesWind {
    $ruta = Join-Path $script:AetherCarpeta 'Wind-Aether\Wind-Aether-perfiles.json'
    if (-not (Test-Path -LiteralPath $ruta)) { return $null }
    return Get-Content -LiteralPath $ruta -Raw -Encoding UTF8 | ConvertFrom-Json
}

function Set-AetherModWind {
    param([string]$Id, [bool]$Activo)
    $cli = Get-AetherWindhawkCli
    if (-not $cli) { return $false }
    $accion = if ($Activo) { 'enable' } else { 'disable' }
    & $cli --quiet mod $accion $Id 2>&1 | Out-Null
    return ($LASTEXITCODE -eq 0)
}

function Set-AetherPerfilWind {
    # Activa los mods del perfil y desactiva los demás mods de Wind-Aether.
    param([ValidateSet('aether-glass', 'aether-limpio', 'ninguno')][string]$Perfil, [switch]$Temporal)
    $perfiles = Get-AetherPerfilesWind
    if (-not $perfiles -or -not (Get-AetherWindhawkCli)) {
        return 'Windhawk no está instalado o no tiene su herramienta de línea de comandos (Windhawk 2.0).'
    }
    $todos = New-Object System.Collections.Generic.HashSet[string]
    foreach ($p in $perfiles.perfiles.PSObject.Properties) { foreach ($id in @($p.Value.activar)) { [void]$todos.Add([string]$id) } }
    $activar = if ($Perfil -eq 'ninguno') { @() } else { @($perfiles.perfiles.$Perfil.activar) }
    $fallos = 0
    foreach ($id in $todos) {
        if (-not (Set-AetherModWind -Id $id -Activo ($activar -contains $id))) { $fallos++ }
    }
    if (-not $Temporal) { Set-AetherAjuste -Nombre 'PerfilWind' -Valor $Perfil }
    if ($Perfil -ne 'ninguno') { [void](Set-AetherColoresWind) }
    if ($fallos -gt 0) { return "Perfil aplicado; $fallos mods no respondieron (quizá no están instalados)." }
    return 'Perfil aplicado.'
}

function Set-AetherPausaModsGaming {
    param([bool]$Pausar)
    $perfiles = Get-AetherPerfilesWind
    if (-not $perfiles -or -not (Get-AetherWindhawkCli)) { return }
    $perfil = [string](Get-AetherAjuste -Nombre 'PerfilWind' -PorDefecto 'ninguno')
    $activos = if ($perfil -eq 'ninguno') { @() } else { @($perfiles.perfiles.$perfil.activar) }
    foreach ($id in @($perfiles.pausarEnModoGaming)) {
        if ($activos -contains $id) { [void](Set-AetherModWind -Id $id -Activo (-not $Pausar)) }
    }
}

# --- Modos -------------------------------------------------------------------------------
function Get-AetherModo {
    return [string](Get-AetherAjuste -Nombre 'Modo' -PorDefecto 'Trabajo')
}

function Set-AetherModo {
    <#
        Cambia a Trabajo, Gaming o Bateria. Devuelve un objeto con Ok y Mensaje.
        -Automatico: lo pidió el vigilante de batería (al conectar el cargador se vuelve
        al modo anterior).
    #>
    param(
        [Parameter(Mandatory = $true)][ValidateSet('Trabajo', 'Gaming', 'Bateria')][string]$Modo,
        [switch]$Automatico
    )
    if ($Modo -eq 'Gaming' -and -not (Test-AetherCargador)) {
        return [pscustomobject]@{ Ok = $false; Mensaje = 'Conecta el cargador para usar el Modo Gaming.' }
    }
    $anterior = Get-AetherModo
    # Trabaja siempre sobre el paquete de energía propio de Aether (no toca los del usuario).
    [void](Initialize-AetherPlanEnergia)
    Save-AetherEnergiaOriginal
    if ($anterior -eq 'Gaming' -and $Modo -ne 'Gaming') { Set-AetherPausaModsGaming -Pausar $false }
    if ($anterior -eq 'Bateria' -and $Modo -ne 'Bateria') {
        [void](Set-AetherPerfilWind -Perfil ([string](Get-AetherAjuste -Nombre 'PerfilWind' -PorDefecto 'ninguno')) -Temporal)
    }

    switch ($Modo) {
        'Trabajo' {
            # Optimizado pero fluido: la CPU responde (hasta 100%) sin quedarse siempre arriba.
            # EPP 50 equilibra rendimiento y temperatura; turbo disponible; sin estacionar núcleos.
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherProcMin -Ac 10 -Dc 5
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherProcMax -Ac 100 -Dc 100
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherMinNucleos -Ac 100 -Dc 100
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherEpp -Ac 50 -Dc 60
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherTurbo -Ac 1 -Dc 1
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherEnfriamiento -Ac 1
            Set-AetherModoEnergiaWindows -Modo Equilibrado
            Set-AetherNotificaciones -Activas $true
            Set-AetherAppsSegundoPlano -Permitidas $true
            $mensaje = 'Modo Trabajo activo.'
        }
        'Gaming' {
            $limitar = (Get-AetherAjuste -Nombre 'LimitarTurbo' -PorDefecto 0) -eq 1
            # Máximo rendimiento: CPU al 100% fijo, sin estacionar núcleos, EPP 0 (todo rendimiento),
            # enfriamiento activo. Con "limitar turbo" la gráfica integrada recibe más energía.
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherProcMin -Ac 100
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherProcMax -Ac 100
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherMinNucleos -Ac 100
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherEpp -Ac 0
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherEnfriamiento -Ac 1
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherTurbo -Ac $(if ($limitar) { 0 } else { 2 })
            Set-AetherModoEnergiaWindows -Modo Rendimiento
            Set-AetherNotificaciones -Activas $false
            Set-AetherAppsSegundoPlano -Permitidas $false
            Set-AetherPausaModsGaming -Pausar $true
            $mensaje = 'Modo Gaming activo. ¿El ventilador está conectado?'
        }
        'Bateria' {
            # Eficiente: con batería la CPU baja el techo y apaga el turbo; enchufada queda normal.
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherProcMax -Dc 80
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherProcMin -Dc 5
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherTurbo -Dc 0
            Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherEpp -Dc 80
            # Ahorro de batería de Windows encendido ya (umbral 100%).
            Set-AetherValorEnergia -Sub $script:AetherSubAhorro -Ajuste $script:AetherUmbralAhorro -Dc 100
            Set-AetherModoEnergiaWindows -Modo Eficiencia
            Set-AetherAppsSegundoPlano -Permitidas $false
            [void](Set-AetherPerfilWind -Perfil 'aether-limpio' -Temporal)
            $mensaje = 'Modo Batería activo.'
        }
    }
    & powercfg.exe /setactive SCHEME_CURRENT 2>&1 | Out-Null

    Set-AetherAjuste -Nombre 'Modo' -Valor $Modo
    if ($Automatico) {
        Set-AetherAjuste -Nombre 'ModoAnterior' -Valor $anterior
        Set-AetherAjuste -Nombre 'ModoAutomatico' -Valor 1
    }
    else {
        Set-AetherAjuste -Nombre 'ModoAutomatico' -Valor 0
    }
    return [pscustomobject]@{ Ok = $true; Mensaje = $mensaje }
}

# --- Utilidades -------------------------------------------------------------------------
function Start-AetherComoAdmin {
    # Abre un programa con permisos de administrador (aviso de UAC). Devuelve $false si se canceló.
    param([Parameter(Mandatory = $true)][string]$Archivo, [string[]]$Argumentos = @(), [switch]$Esperar)
    try {
        $parametros = @{ FilePath = $Archivo; Verb = 'RunAs'; ErrorAction = 'Stop' }
        if ($Argumentos.Count -gt 0) { $parametros['ArgumentList'] = $Argumentos }
        if ($Esperar) { $parametros['Wait'] = $true }
        Start-Process @parametros
        return $true
    }
    catch {
        return $false
    }
}

function Get-AetherLanzador {
    # Ruta de un acceso directo de la carpeta Aether (C:\Windows\AtlasDesktop).
    param([Parameter(Mandatory = $true)][string]$Relativa)
    return Join-Path $script:AetherEscritorio $Relativa
}

function Invoke-AetherLanzador {
    param([Parameter(Mandatory = $true)][string]$Relativa, [switch]$Silencioso)
    $ruta = Get-AetherLanzador -Relativa $Relativa
    if (-not (Test-Path -LiteralPath $ruta)) { return $false }
    $argumentos = @('/c', "`"$ruta`"")
    if ($Silencioso) { $argumentos += '/silent' }
    return Start-AetherComoAdmin -Archivo 'cmd.exe' -Argumentos $argumentos -Esperar
}

function Clear-AetherTemporales {
    # Limpieza segura: archivos temporales del usuario y la papelera. Nunca toca Descargas,
    # Escritorio ni Documentos.
    $antes = 0
    $carpeta = [IO.Path]::GetTempPath()
    $archivos = @(Get-ChildItem -LiteralPath $carpeta -Recurse -Force -File -ErrorAction SilentlyContinue)
    foreach ($archivo in $archivos) { $antes += $archivo.Length }
    foreach ($elemento in @(Get-ChildItem -LiteralPath $carpeta -Force -ErrorAction SilentlyContinue)) {
        Remove-Item -LiteralPath $elemento.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
    $quedan = 0
    foreach ($archivo in @(Get-ChildItem -LiteralPath $carpeta -Recurse -Force -File -ErrorAction SilentlyContinue)) { $quedan += $archivo.Length }
    try { Clear-RecycleBin -Force -ErrorAction Stop } catch { Write-Verbose 'La papelera ya estaba vacía.' }
    return [Math]::Round(($antes - $quedan) / 1MB)
}

function Get-AetherSaludBateria {
    try {
        $diseno = @(Get-CimInstance -Namespace root\wmi -ClassName BatteryStaticData -ErrorAction Stop)[0].DesignedCapacity
        $actual = @(Get-CimInstance -Namespace root\wmi -ClassName BatteryFullChargedCapacity -ErrorAction Stop)[0].FullChargedCapacity
        if ($diseno -gt 0) {
            return [pscustomobject]@{ Diseno = $diseno; Actual = $actual; Salud = [Math]::Round(100 * $actual / $diseno) }
        }
    }
    catch {
        Write-Verbose "Sin datos de batería: $($_.Exception.Message)"
    }
    return $null
}

# --- Unidades de red fijas y confianza para Excel ---------------------------------------
$script:AetherUnidadesArchivo = Join-Path $script:AetherDatos 'unidades.json'

function Get-AetherUnidades {
    if (-not (Test-Path -LiteralPath $script:AetherUnidadesArchivo)) { return @() }
    try { return @(Get-Content -LiteralPath $script:AetherUnidadesArchivo -Raw -Encoding UTF8 | ConvertFrom-Json) }
    catch { return @() }
}

function Save-AetherUnidades {
    param([object[]]$Unidades)
    New-Item -ItemType Directory -Path $script:AetherDatos -Force | Out-Null
    ConvertTo-Json -InputObject @($Unidades) | Set-Content -LiteralPath $script:AetherUnidadesArchivo -Encoding UTF8
}

function Get-AetherEquipoDeRuta {
    # \\192.168.1.10\Carpeta -> 192.168.1.10
    param([string]$Ruta)
    if ($Ruta -match '^\\\\([^\\]+)\\') { return $Matches[1] }
    return $null
}

function Add-AetherSitioConfianza {
    # La PC de la red queda en "Intranet local" (sin avisos al abrir archivos) y su carpeta
    # en las ubicaciones de confianza de Excel (las macros funcionan sin bloquearse).
    param([Parameter(Mandatory = $true)][string]$Ruta, [string]$Letra)
    $equipo = Get-AetherEquipoDeRuta -Ruta $Ruta
    if ($equipo) {
        if ($equipo -match '^\d{1,3}(\.\d{1,3}){3}$') {
            $base = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\Ranges'
            $clave = Join-Path $base ("Aether-" + ($equipo -replace '\.', '-'))
            New-Item -Path $clave -Force | Out-Null
            Set-ItemProperty -LiteralPath $clave -Name ':Range' -Value $equipo -Type String
            Set-ItemProperty -LiteralPath $clave -Name 'file' -Value 1 -Type DWord
        }
        else {
            $clave = Join-Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap\Domains' $equipo
            New-Item -Path $clave -Force | Out-Null
            Set-ItemProperty -LiteralPath $clave -Name 'file' -Value 1 -Type DWord
        }
    }
    $confianza = 'HKCU:\Software\Microsoft\Office\16.0\Excel\Security\Trusted Locations'
    New-Item -Path $confianza -Force | Out-Null
    Set-ItemProperty -LiteralPath $confianza -Name 'AllowNetworkLocations' -Value 1 -Type DWord
    $rutas = @($Ruta.TrimEnd('\') + '\')
    if ($Letra) { $rutas += "$($Letra.TrimEnd(':')):\" }
    foreach ($ruta in $rutas) {
        $existe = @(Get-ChildItem -LiteralPath $confianza -ErrorAction SilentlyContinue | Where-Object {
                (Get-ItemProperty -LiteralPath $_.PSPath -Name Path -ErrorAction SilentlyContinue).Path -eq $ruta
            })
        if ($existe.Count -gt 0) { continue }
        $n = 1
        while (Test-Path -LiteralPath (Join-Path $confianza "AetherLocation$n")) { $n++ }
        $clave = Join-Path $confianza "AetherLocation$n"
        New-Item -Path $clave -Force | Out-Null
        Set-ItemProperty -LiteralPath $clave -Name 'Path' -Value $ruta -Type String
        Set-ItemProperty -LiteralPath $clave -Name 'AllowSubfolders' -Value 1 -Type DWord
        Set-ItemProperty -LiteralPath $clave -Name 'Description' -Value 'Aether OS - carpeta de red' -Type String
    }
}

function Connect-AetherUnidadRed {
    # Conecta (o reconecta) una unidad. Devuelve $true si quedó disponible.
    param([Parameter(Mandatory = $true)][string]$Letra, [Parameter(Mandatory = $true)][string]$Ruta)
    $letra = "$($Letra.TrimEnd(':').ToUpperInvariant()):"
    if (Test-Path -LiteralPath "$letra\") { return $true }
    & net.exe use $letra /delete /y 2>&1 | Out-Null
    & net.exe use $letra $Ruta /persistent:yes 2>&1 | Out-Null
    return (Test-Path -LiteralPath "$letra\")
}

function Add-AetherUnidadRed {
    param([Parameter(Mandatory = $true)][string]$Letra, [Parameter(Mandatory = $true)][string]$Ruta)
    $Ruta = $Ruta.Trim().TrimEnd('\')
    if ($Ruta -notmatch '^\\\\[^\\]+\\[^\\]+') { throw 'La ruta debe ser como \\192.168.1.10\Carpeta (IP de la otra PC y carpeta compartida).' }
    $letra = $Letra.TrimEnd(':').ToUpperInvariant()
    $unidades = @(Get-AetherUnidades | Where-Object { $_.Letra -ne $letra })
    $unidades += [pscustomobject]@{ Letra = $letra; Ruta = $Ruta }
    Save-AetherUnidades -Unidades $unidades
    Add-AetherSitioConfianza -Ruta $Ruta -Letra $letra
    return (Connect-AetherUnidadRed -Letra $letra -Ruta $Ruta)
}

function Remove-AetherUnidadRed {
    param([Parameter(Mandatory = $true)][string]$Letra)
    $letra = $Letra.TrimEnd(':').ToUpperInvariant()
    Save-AetherUnidades -Unidades @(Get-AetherUnidades | Where-Object { $_.Letra -ne $letra })
    & net.exe use "$($letra):" /delete /y 2>&1 | Out-Null
}

function Connect-AetherUnidadesRed {
    # Reconecta las unidades que se cayeron. Devuelve las que siguen sin conectar.
    $sinConectar = @()
    foreach ($unidad in @(Get-AetherUnidades)) {
        if (-not (Connect-AetherUnidadRed -Letra $unidad.Letra -Ruta $unidad.Ruta)) { $sinConectar += "$($unidad.Letra): $($unidad.Ruta)" }
    }
    return $sinConectar
}

# --- Tema y colores de Wind-Aether --------------------------------------------------------
function Get-AetherTema {
    # 'claro' u 'oscuro' según el modo de las apps de Windows.
    try {
        $claro = Get-ItemPropertyValue -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' -Name AppsUseLightTheme -ErrorAction Stop
        if ($claro -eq 1) { return 'claro' }
    }
    catch { Write-Verbose 'Sin preferencia de tema; se usa oscuro.' }
    return 'oscuro'
}

function Set-AetherColoresWind {
    # Pone en los mods de Wind-Aether los colores del tema (claro u oscuro).
    param([ValidateSet('claro', 'oscuro')][string]$Tema = (Get-AetherTema))
    $perfiles = Get-AetherPerfilesWind
    $cli = Get-AetherWindhawkCli
    if (-not $perfiles -or -not $cli -or -not $perfiles.PSObject.Properties['coloresPorTema']) { return 0 }
    $hechos = 0
    foreach ($mod in $perfiles.coloresPorTema.PSObject.Properties) {
        $valores = $mod.Value.$Tema
        if (-not $valores) { continue }
        $pares = @($valores.PSObject.Properties | ForEach-Object { '{0}={1}' -f $_.Name, $_.Value })
        if ($pares.Count -eq 0) { continue }
        & $cli --quiet mod settings set $mod.Name @pares 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) { $hechos++ }
    }
    Set-AetherAjuste -Nombre 'TemaColores' -Valor $Tema
    return $hechos
}

function Set-AetherTema {
    # Cambia Windows a claro u oscuro y ajusta los colores de Wind-Aether.
    param([Parameter(Mandatory = $true)][ValidateSet('claro', 'oscuro')][string]$Tema)
    $valor = [int]($Tema -eq 'claro')
    $ruta = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize'
    if (-not (Test-Path -LiteralPath $ruta)) { New-Item -Path $ruta -Force | Out-Null }
    Set-ItemProperty -LiteralPath $ruta -Name AppsUseLightTheme -Value $valor -Type DWord
    Set-ItemProperty -LiteralPath $ruta -Name SystemUsesLightTheme -Value $valor -Type DWord
    [void](Set-AetherColoresWind -Tema $Tema)
}
