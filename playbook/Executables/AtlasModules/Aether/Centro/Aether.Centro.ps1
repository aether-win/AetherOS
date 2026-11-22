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

function Set-AetherAnimaciones {
    # Animaciones completas de Windows 11 ($Completas = $true) o reducidas para más
    # rendimiento ($false). Aether las deja completas siempre, menos en el Modo Gaming.
    param([bool]$Completas)
    $v = if ($Completas) { 1 } else { 0 }
    Set-ItemProperty -LiteralPath 'HKCU:\Control Panel\Desktop\WindowMetrics' -Name 'MinAnimate' -Value "$v" -Type String -ErrorAction SilentlyContinue
    $adv = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced'
    if (-not (Test-Path -LiteralPath $adv)) { New-Item -Path $adv -Force | Out-Null }
    Set-ItemProperty -LiteralPath $adv -Name 'TaskbarAnimations' -Value $v -Type DWord -ErrorAction SilentlyContinue
    Set-ItemProperty -LiteralPath 'HKCU:\SOFTWARE\Microsoft\Windows\DWM' -Name 'EnableAeroPeek' -Value $v -Type DWord -ErrorAction SilentlyContinue
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

# =========================================================================================
# Automatización (Aether Toolbox la corre sola al iniciar sesión): drivers de mandos,
# Windhawk 2.0 con windhawk-cli y los mods de Wind-Aether. Todo con firma verificada y
# registro en %ProgramData%\Aether\autoreparacion.log.
# =========================================================================================
function Write-AetherAutoLog {
    param([string]$Texto)
    try {
        $carpeta = Join-Path $env:ProgramData 'Aether'
        if (-not (Test-Path -LiteralPath $carpeta)) { New-Item -ItemType Directory -Path $carpeta -Force | Out-Null }
        $log = Join-Path $carpeta 'autoreparacion.log'
        if ((Test-Path -LiteralPath $log) -and (Get-Item -LiteralPath $log).Length -gt 1MB) { Remove-Item -LiteralPath $log -Force }
        Add-Content -LiteralPath $log -Value ('[{0:yyyy-MM-dd HH:mm:ss}] {1}' -f (Get-Date), $Texto) -Encoding UTF8
    }
    catch { Write-Verbose 'Sin registro.' }
}

function Get-AetherReleaseAsset {
    # URL del primer archivo que cumple $Patron en la versión más reciente (oficial) de un repo de GitHub.
    param([string]$Repo, [string]$Patron, [string]$PatronTag = '.', [switch]$Previas)
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $versiones = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases?per_page=30" `
        -Headers @{ 'User-Agent' = 'AetherOS' } -UseBasicParsing -ErrorAction Stop
    $lista = @($versiones | Where-Object { -not $_.draft -and ($Previas -or -not $_.prerelease) -and $_.tag_name -match $PatronTag }) |
        Sort-Object { [datetime]$_.published_at } -Descending
    foreach ($v in $lista) {
        $a = @($v.assets | Where-Object { $_.name -match $Patron }) | Select-Object -First 1
        if ($a) { return $a.browser_download_url }
    }
    return $null
}

function Get-AetherArgsSilencio {
    # Argumentos silenciosos correctos según la tecnología del instalador.
    param([string]$Archivo)
    $tipoGuardado = "$Archivo.tipo"
    $tipo = if (Test-Path -LiteralPath $tipoGuardado) { (Get-Content -LiteralPath $tipoGuardado -Raw).Trim() } else {
        $texto = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($Archivo))
        if ($texto.Contains('.wixburn')) { 'WiX' } elseif ($texto.Contains('Advanced Installer')) { 'AdvancedInstaller' } elseif ($texto.Contains('Inno Setup')) { 'Inno' } else { 'Desconocido' }
    }
    switch ($tipo) {
        'WiX' { return @('/quiet /norestart') }
        'AdvancedInstaller' { return @('/exenoui /qn /norestart') }
        'Inno' { return @('/VERYSILENT /SUPPRESSMSGBOXES /NORESTART') }
        default { return @('/quiet /norestart', '/exenoui /qn /norestart', '/S') }
    }
}

function Invoke-AetherInstalador {
    # Ejecuta un instalador ya descargado/incluido: firma válida obligatoria, argumentos según su tipo
    # y tiempo límite (si se queda esperando, se cierra en vez de colgarse para siempre).
    param([string]$Archivo, [scriptblock]$Listo, [string]$Nombre, [int]$Minutos = 6)
    $firma = Get-AuthenticodeSignature -LiteralPath $Archivo
    if ($firma.Status -ne 'Valid') { Write-AetherAutoLog "${Nombre}: firma NO válida ($($firma.Status)); no se instala."; return $false }
    foreach ($argumentos in (Get-AetherArgsSilencio -Archivo $Archivo)) {
        $p = Start-Process -FilePath $Archivo -ArgumentList $argumentos -PassThru -WindowStyle Hidden
        if (-not $p.WaitForExit($Minutos * 60000)) {
            try { $p.Kill() } catch { Write-Verbose 'Ya terminó.' }
            Write-AetherAutoLog "${Nombre}: '$argumentos' no terminó en $Minutos min; se cerró."
            continue
        }
        Write-AetherAutoLog "${Nombre}: instalador '$argumentos' -> código $($p.ExitCode)"
        for ($i = 0; $i -lt 10; $i++) { if (& $Listo) { return $true }; Start-Sleep -Seconds 2 }
    }
    return [bool](& $Listo)
}

function Invoke-AetherInstaladorFirmado {
    # Descarga un instalador oficial y lo ejecuta con Invoke-AetherInstalador.
    param([string]$Url, [scriptblock]$Listo, [string]$Nombre)
    $archivo = Join-Path $env:TEMP ('aether-' + [guid]::NewGuid().ToString('N') + '.exe')
    try {
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $Url -OutFile $archivo -UseBasicParsing -ErrorAction Stop
        return (Invoke-AetherInstalador -Archivo $archivo -Listo $Listo -Nombre $Nombre)
    }
    catch { Write-AetherAutoLog "${Nombre}: error $($_.Exception.Message)"; return $false }
    finally { Remove-Item -LiteralPath $archivo -Force -ErrorAction SilentlyContinue }
}

function Install-AetherDriversMando {
    # ViGEmBus (mando virtual) y HidHide (oculta el mando real): solo si faltan.
    # 1) Instalador oficial incluido en Aether (sin Internet), 2) GitHub de Nefarius, 3) winget.
    $resultado = @()
    $incluidos = Join-Path $script:AetherCarpeta 'Drivers'
    foreach ($d in @(
            @{ Nombre = 'ViGEmBus'; Servicio = 'ViGEmBus'; Archivo = 'ViGEmBus-Setup.exe'; Repo = 'nefarius/ViGEmBus'; Patron = '(?i)vigembus.*\.exe$'; Winget = 'Nefarius.ViGEmBus' },
            @{ Nombre = 'HidHide'; Servicio = 'HidHide'; Archivo = 'HidHide-Setup.exe'; Repo = 'nefarius/HidHide'; Patron = '(?i)hidhide.*x64.*\.exe$'; Winget = 'Nefarius.HidHide' }
        )) {
        $servicio = $d.Servicio
        $listo = { [bool](Get-Service -Name $servicio -ErrorAction SilentlyContinue) }.GetNewClosure()
        if (& $listo) { continue }
        Write-AetherAutoLog "$($d.Nombre): no está; se instala."
        $ok = $false
        $local = Join-Path $incluidos $d.Archivo
        if (Test-Path -LiteralPath $local) { $ok = Invoke-AetherInstalador -Archivo $local -Listo $listo -Nombre $d.Nombre }
        if (-not $ok) {
            try {
                $url = Get-AetherReleaseAsset -Repo $d.Repo -Patron $d.Patron
                if ($url) { $ok = Invoke-AetherInstaladorFirmado -Url $url -Nombre $d.Nombre -Listo $listo }
            }
            catch { Write-AetherAutoLog "$($d.Nombre): GitHub no respondió ($($_.Exception.Message))." }
        }
        if (-not $ok) {
            $winget = Get-Command winget.exe -ErrorAction SilentlyContinue
            if ($winget) {
                & $winget.Source install --id $d.Winget --exact --silent --accept-package-agreements --accept-source-agreements --disable-interactivity *> $null
                $ok = & $listo
                Write-AetherAutoLog "$($d.Nombre): winget -> $ok"
            }
        }
        $resultado += if ($ok) { "$($d.Nombre) instalado" } else { "$($d.Nombre) NO se pudo instalar" }
    }
    return $resultado
}

function Use-AetherCandadoWindhawk {
    # Un solo trabajo con Windhawk a la vez (bienvenida, Toolbox y autoreparación no se pisan).
    param([scriptblock]$Bloque, [int]$Minutos = 30)
    $m = New-Object System.Threading.Mutex($false, 'Global\AetherWindhawk')
    $tengo = $false
    try {
        try { $tengo = $m.WaitOne($Minutos * 60000) } catch [System.Threading.AbandonedMutexException] { $tengo = $true }
        if (-not $tengo) { Write-AetherAutoLog 'Windhawk ocupado demasiado tiempo; se omite.'; return $null }
        & $Bloque
    }
    finally {
        if ($tengo) { $m.ReleaseMutex() }
        $m.Dispose()
    }
}

function Set-AetherProgresoWind {
    param([string]$Texto)
    $archivo = Join-Path $env:ProgramData 'Aether\windhawk-progreso.txt'
    try {
        if ($Texto) { Set-Content -LiteralPath $archivo -Value $Texto -Encoding UTF8 }
        else { Remove-Item -LiteralPath $archivo -Force -ErrorAction SilentlyContinue }
    }
    catch { Write-Verbose 'Sin progreso.' }
}

function Install-AetherWindMods {
    # Instala en Windhawk (apagados) los mods de Wind-Aether que falten, con los ajustes de Aether.
    # Mods del repositorio: precompilados. Mods propios (local@): desde su código fuente.
    param([string]$Solo)
    Use-AetherCandadoWindhawk {
    $cli = Get-AetherWindhawkCli
    if (-not $cli) { return @('Windhawk sin windhawk-cli') }
    $paquete = Join-Path $script:AetherCarpeta 'Wind-Aether\Wind-Aether-mods.json'
    $datos = Get-Content -LiteralPath $paquete -Raw -Encoding UTF8 | ConvertFrom-Json
    $instalados = @()
    try { $instalados = @(((& $cli mod list --json 2>$null) | Out-String | ConvertFrom-Json) | ForEach-Object { $_.id }) }
    catch { Write-AetherAutoLog 'Windhawk: no se pudo leer la lista de mods.' }
    $fallos = @()
    $pendientes = @($datos.mods | Where-Object { (-not $Solo -or $_.modId -eq $Solo) -and $instalados -notcontains $_.modId }).Count
    $n = 0
    foreach ($m in @($datos.mods)) {
        if ($Solo -and $m.modId -ne $Solo) { continue }
        if ($instalados -contains $m.modId) { continue }
        $n++
        Set-AetherProgresoWind ("Instalando en Windhawk: {0} ({1} de {2})" -f $m.modId, $n, $pendientes)
        if ($m.modId -like 'local@*') {
            $archivo = Join-Path $env:TEMP (($m.modId -replace '^local@', '') + '.wh.cpp')
            [IO.File]::WriteAllText($archivo, [string]$m.source, (New-Object Text.UTF8Encoding($false)))
            & $cli mod install --file $archivo --disabled --yes *> $null
            Remove-Item -LiteralPath $archivo -Force -ErrorAction SilentlyContinue
        }
        else {
            & $cli mod install --repo $m.modId --disabled --yes *> $null
        }
        $codigo = $LASTEXITCODE
        Write-AetherAutoLog "Windhawk: instalar $($m.modId) -> código $codigo"
        if ($codigo -ne 0 -and $codigo -ne 7) { $fallos += "$($m.modId) (código $codigo)"; continue }
        $pares = @()
        if ($m.settings) {
            foreach ($p in $m.settings.PSObject.Properties) {
                if ($null -ne $p.Value -and "$($p.Value)" -ne '') { $pares += ('{0}={1}' -f $p.Name, $p.Value) }
            }
        }
        if ($pares.Count) {
            & $cli mod settings set $m.modId @pares *> $null
            Write-AetherAutoLog "Windhawk: ajustes de $($m.modId) -> código $LASTEXITCODE"
        }
    }
    Set-AetherProgresoWind ''
    return $fallos
    }
}

function Invoke-AetherAutoReparacion {
    # Deja Aether OS completo sin intervención: drivers de mandos, Windhawk 2.0 y mods.
    # Mientras el instalador de apps del primer inicio no termine, no se cruza con él.
    $enCurso = @(Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" -ErrorAction SilentlyContinue |
            Where-Object { $_.CommandLine -match 'Install-AetherApps' })
    if ($enCurso.Count) { Write-AetherAutoLog 'El instalador de apps del primer inicio sigue trabajando; se reintenta en el próximo inicio de sesión.'; return @() }
    $lineas = @()
    $lineas += Install-AetherDriversMando
    if (-not (Get-AetherWindhawkCli)) {
        if (Install-AetherWindhawkCli) { $lineas += 'Windhawk 2.0 instalado' }
        else { $lineas += 'Windhawk 2.0 NO se pudo instalar' }
    }
    if (Get-AetherWindhawkCli) {
        $fallos = @(Install-AetherWindMods)
        if ($fallos.Count) { $lineas += ('Mods de Wind-Aether con problemas: ' + ($fallos -join ', ')) }
    }
    Write-AetherAutoLog ('Resumen: ' + ($(if ($lineas.Count) { $lineas -join ' | ' } else { 'todo al día' })))
    return $lineas
}

function Install-AetherWindhawkCli {
    <#
        Instala Windhawk 2.0 (trae windhawk-cli.exe, necesario para que Aether Toolbox active
        los perfiles Wind-Aether solo). Baja el instalador OFICIAL de las versiones de
        github.com/ramensoftware/windhawk, exige firma digital válida y lo instala en silencio.
        Devuelve la ruta de windhawk-cli.exe o $null (entonces se usa la 1.x estable).
    #>
    $cli = Get-AetherWindhawkCli
    if ($cli) { return $cli }
    $setup = Join-Path $env:TEMP ('windhawk2-' + [guid]::NewGuid().ToString('N') + '.exe')
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $versiones = Invoke-RestMethod -Uri 'https://api.github.com/repos/ramensoftware/windhawk/releases?per_page=30' `
            -Headers @{ 'User-Agent' = 'AetherOS' } -UseBasicParsing -ErrorAction Stop
        $version = @($versiones | Where-Object { $_.tag_name -match '^v?2\.' -and -not $_.draft }) |
            Sort-Object { [datetime]$_.published_at } -Descending | Select-Object -First 1
        if (-not $version) { return $null }
        # El instalador completo (offline) trae también las herramientas para compilar los mods
        # propios de Wind-Aether (local@...); si no está, el normal.
        $activo = @($version.assets | Where-Object { $_.name -match '(?i)setup_offline.*\.exe$' }) | Select-Object -First 1
        if (-not $activo) { $activo = @($version.assets | Where-Object { $_.name -match '(?i)setup.*\.exe$' }) | Select-Object -First 1 }
        if (-not $activo) { return $null }
        Invoke-WebRequest -Uri $activo.browser_download_url -OutFile $setup -UseBasicParsing -ErrorAction Stop
        $firma = Get-AuthenticodeSignature -LiteralPath $setup
        if ($firma.Status -ne 'Valid') { return $null }
        $p = Start-Process -FilePath $setup -ArgumentList '/S' -PassThru -Wait
        if ($p.ExitCode -ne 0) { return $null }
        for ($i = 0; $i -lt 10 -and -not (Get-AetherWindhawkCli); $i++) { Start-Sleep -Seconds 2 }
        return (Get-AetherWindhawkCli)
    }
    catch { return $null }
    finally { Remove-Item -LiteralPath $setup -Force -ErrorAction SilentlyContinue }
}

function Get-AetherPerfilesWind {
    $ruta = Join-Path $script:AetherCarpeta 'Wind-Aether\Wind-Aether-perfiles.json'
    if (-not (Test-Path -LiteralPath $ruta)) { return $null }
    return Get-Content -LiteralPath $ruta -Raw -Encoding UTF8 | ConvertFrom-Json
}

function Set-AetherModWind {
    # Activa/desactiva un mod de Wind-Aether. Si el mod todavía no está en Windhawk, lo instala
    # primero (con sus ajustes de Aether). Devuelve $true solo si Windhawk confirmó el cambio.
    param([string]$Id, [bool]$Activo)
    $r = Use-AetherCandadoWindhawk {
    $cli = Get-AetherWindhawkCli
    if (-not $cli) { Write-AetherAutoLog 'Windhawk: falta windhawk-cli.exe (Windhawk 2.0).'; return $false }
    $accion = if ($Activo) { 'enable' } else { 'disable' }
    & $cli mod $accion $Id *> $null
    $codigo = $LASTEXITCODE
    if ($codigo -eq 4 -and $Activo) {
        [void](Install-AetherWindMods -Solo $Id)
        & $cli mod $accion $Id *> $null
        $codigo = $LASTEXITCODE
    }
    Write-AetherAutoLog "Windhawk: mod $accion $Id -> código $codigo"
    # Apagar un mod que ni siquiera está instalado = ya está apagado.
    return ($codigo -eq 0 -or ($codigo -eq 4 -and -not $Activo))
    }
    return [bool]($r | Select-Object -Last 1)
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

# --- Intensidad del Modo Gaming (Gaming / Gaming Extremo / Máximo Rendimiento) -----------
# Cada extra es reversible: antes de aplicarlo se guarda el valor que había y al salir de
# Gaming se pone de vuelta. Descripción, beneficio, justificación y riesgo de cada uno en
# docs/aether/optimizaciones-gaming.md (y en la sección Modos de Aether Toolbox).
$script:AetherSubUsb = '2a737441-1930-4402-8d77-b2bebba308a3'
$script:AetherUsbSuspension = '48e6b7a6-50f5-4782-a5d4-53bb8f07e226'   # USBSELECTIVESUSPEND
$script:AetherSubPcie = '501a4d13-42af-4429-9fd1-a8218c268e20'
$script:AetherPcieAspm = 'ee12f906-d277-404b-b6da-e5fa1a576df5'         # ASPM
$script:AetherIdleOff = '5d76a2ca-e8c0-402f-a133-2158492d58ad'          # IDLEDISABLE
$script:AetherClaveThrottling = 'HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling'

function Save-AetherExtraEnergia {
    param([string]$Clave, [string]$Sub, [string]$Ajuste)
    if ([string](Get-AetherAjuste -Nombre "Extra$Clave" -PorDefecto '') -ne '') { return }
    $valores = Get-AetherValorEnergia -Sub $Sub -Ajuste $Ajuste
    if ($null -ne $valores) { Set-AetherAjuste -Nombre "Extra$Clave" -Valor "$($valores[0]),$($valores[1])" }
}

function Set-AetherNivelGaming {
    <# Aplica los extras del nivel elegido encima del Modo Gaming. #>
    param([ValidateSet('Gaming', 'Extremo', 'Maximo')][string]$Nivel)
    if ($Nivel -eq 'Gaming') { return }

    # Extremo y Máximo: menos latencia de USB y PCIe, sin indexado ni recorte de energía a apps.
    Save-AetherExtraEnergia -Clave 'Usb' -Sub $script:AetherSubUsb -Ajuste $script:AetherUsbSuspension
    Set-AetherValorEnergia -Sub $script:AetherSubUsb -Ajuste $script:AetherUsbSuspension -Ac 0
    Save-AetherExtraEnergia -Clave 'Pcie' -Sub $script:AetherSubPcie -Ajuste $script:AetherPcieAspm
    Set-AetherValorEnergia -Sub $script:AetherSubPcie -Ajuste $script:AetherPcieAspm -Ac 0

    $busqueda = Get-Service -Name 'WSearch' -ErrorAction SilentlyContinue
    if ($busqueda -and $busqueda.Status -eq 'Running') {
        Set-AetherAjuste -Nombre 'ExtraBusqueda' -Valor 1
        Stop-Service -Name 'WSearch' -Force -ErrorAction SilentlyContinue
    }

    if ([string](Get-AetherAjuste -Nombre 'ExtraThrottling' -PorDefecto '') -eq '') {
        $antes = (Get-ItemProperty -LiteralPath $script:AetherClaveThrottling -Name 'PowerThrottlingOff' -ErrorAction SilentlyContinue).PowerThrottlingOff
        Set-AetherAjuste -Nombre 'ExtraThrottling' -Valor $(if ($null -eq $antes) { 'ninguno' } else { [string]$antes })
    }
    if (-not (Test-Path -LiteralPath $script:AetherClaveThrottling)) { New-Item -Path $script:AetherClaveThrottling -Force | Out-Null }
    Set-ItemProperty -LiteralPath $script:AetherClaveThrottling -Name 'PowerThrottlingOff' -Value 1 -Type DWord

    if ($Nivel -eq 'Maximo') {
        # Máximo: la CPU no entra en reposo (menos latencia, MÁS CALOR) y turbo agresivo siempre.
        Save-AetherExtraEnergia -Clave 'Idle' -Sub $script:AetherSubProcesador -Ajuste $script:AetherIdleOff
        Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherIdleOff -Ac 1
        Set-AetherValorEnergia -Sub $script:AetherSubProcesador -Ajuste $script:AetherTurbo -Ac 2
    }
    Set-AetherAjuste -Nombre 'NivelAplicado' -Valor $Nivel
}

function Restore-AetherNivelGaming {
    <# Deshace los extras de Gaming Extremo / Máximo Rendimiento (valores guardados). #>
    foreach ($e in @(
            @{ Clave = 'Usb'; Sub = $script:AetherSubUsb; Ajuste = $script:AetherUsbSuspension }
            @{ Clave = 'Pcie'; Sub = $script:AetherSubPcie; Ajuste = $script:AetherPcieAspm }
            @{ Clave = 'Idle'; Sub = $script:AetherSubProcesador; Ajuste = $script:AetherIdleOff }
        )) {
        $guardado = [string](Get-AetherAjuste -Nombre "Extra$($e.Clave)" -PorDefecto '')
        if ($guardado -match '^(\d+),(\d+)$') {
            Set-AetherValorEnergia -Sub $e.Sub -Ajuste $e.Ajuste -Ac $Matches[1] -Dc $Matches[2]
        }
        Set-AetherAjuste -Nombre "Extra$($e.Clave)" -Valor ''
    }
    if ((Get-AetherAjuste -Nombre 'ExtraBusqueda' -PorDefecto 0) -eq 1) {
        Start-Service -Name 'WSearch' -ErrorAction SilentlyContinue
        Set-AetherAjuste -Nombre 'ExtraBusqueda' -Valor 0
    }
    $throttling = [string](Get-AetherAjuste -Nombre 'ExtraThrottling' -PorDefecto '')
    if ($throttling -eq 'ninguno') {
        Remove-ItemProperty -LiteralPath $script:AetherClaveThrottling -Name 'PowerThrottlingOff' -ErrorAction SilentlyContinue
    }
    elseif ($throttling -match '^\d+$') {
        Set-ItemProperty -LiteralPath $script:AetherClaveThrottling -Name 'PowerThrottlingOff' -Value ([int]$throttling) -Type DWord
    }
    Set-AetherAjuste -Nombre 'ExtraThrottling' -Valor ''
    Set-AetherAjuste -Nombre 'NivelAplicado' -Valor 'Gaming'
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
    # Los extras de Gaming Extremo / Máximo se deshacen al salir (o antes de re-aplicar Gaming).
    if ($anterior -eq 'Gaming') { Restore-AetherNivelGaming }
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
            Set-AetherAnimaciones -Completas $true
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
            Set-AetherAnimaciones -Completas $false
            Set-AetherPausaModsGaming -Pausar $true
            $nivel = [string](Get-AetherAjuste -Nombre 'NivelGaming' -PorDefecto 'Gaming')
            if ($nivel -notin @('Gaming', 'Extremo', 'Maximo')) { $nivel = 'Gaming' }
            Set-AetherNivelGaming -Nivel $nivel
            $mensaje = switch ($nivel) {
                'Extremo' { 'Modo Gaming Extremo activo. ¿El ventilador está conectado?' }
                'Maximo' { 'Modo Máximo Rendimiento activo: la laptop se calentará más. Usa el ventilador.' }
                default { 'Modo Gaming activo. ¿El ventilador está conectado?' }
            }
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
            Set-AetherAnimaciones -Completas $true
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
