<#
    Guardián de Aether: vuelve a aplicar los ajustes de Aether OS si algo los quitó
    (reinicio, Windows Update, una red nueva, otro programa). No cambia nada que ya esté bien.

    -Sistema : tarea \Aether\Guardian (SYSTEM) al arrancar, al conectarse a una red y cada día:
               red de trabajo completa (aether-red) + ajustes HKLM de los tweaks de Aether.
    -Usuario : lo corre Aether Toolbox al iniciar sesión: ajustes HKCU (+ HKLM) del usuario.

    Respeta las decisiones: el tema de Office lo maneja Aether Toolbox, y los tweaks que dependen
    de una opción del instalador (p. ej. SmartScreen con "Defender desactivado") solo se reponen
    si esa opción se eligió. Registro: %ProgramData%\Aether\guardian.log
#>
param([switch]$Sistema, [switch]$Usuario)
$ErrorActionPreference = 'Continue'

$carpetaDatos = Join-Path $env:ProgramData 'Aether'
if (-not (Test-Path -LiteralPath $carpetaDatos)) { New-Item -ItemType Directory -Path $carpetaDatos -Force | Out-Null }
$log = Join-Path $carpetaDatos 'guardian.log'
if ((Test-Path -LiteralPath $log) -and (Get-Item -LiteralPath $log).Length -gt 1MB) { Remove-Item -LiteralPath $log -Force }
function Write-Guardian { param([string]$Texto) Add-Content -LiteralPath $log -Value ('[{0:yyyy-MM-dd HH:mm:ss}] {1}' -f (Get-Date), $Texto) -Encoding UTF8 }

# Los scripts de tweaks usan Write-AtlasLog del instalador; fuera de él va al registro del guardián.
if (-not (Get-Command Write-AtlasLog -ErrorAction SilentlyContinue)) {
    function global:Write-AtlasLog { param([string]$Message, [string]$Level = 'Info', $ErrorRecord) Write-Guardian "[$Level] $Message" }
}

$modulos = Join-Path ([Environment]::GetFolderPath('Windows')) 'AtlasModules'
$tweaks = Join-Path $modulos 'Scripts\Tweaks'

# Ajustes que NO repone el guardián (los maneja Aether Toolbox o el usuario los cambia a propósito).
$omitir = @('HKCU\Software\Microsoft\Office\16.0\Common|UI Theme')

function Test-OpcionElegida {
    # ¿Se eligió esta opción al instalar? (estado de instalación de Aether: recordedOptions).
    # Si no se puede saber, NO se repone (lo seguro).
    param([string]$Opcion)
    if (-not $Opcion) { return $true }
    $carpeta = Join-Path ([Environment]::GetFolderPath('Windows')) 'AtlasOS\Install'
    foreach ($json in Get-ChildItem -LiteralPath $carpeta -Filter '*.json' -ErrorAction SilentlyContinue) {
        try {
            $estado = Get-Content -LiteralPath $json.FullName -Raw -ErrorAction Stop | ConvertFrom-Json
            if (@($estado.recordedOptions) -contains $Opcion) { return $true }
        }
        catch { continue }
    }
    return $false
}

function Set-AetherValor {
    param([string]$Ruta, [string]$Nombre, [string]$Tipo, $Dato)
    $raiz = $Ruta -replace '^HKLM\\', 'HKEY_LOCAL_MACHINE\' -replace '^HKCU\\', 'HKEY_CURRENT_USER\'
    $lp = "Registry::$raiz"
    $actual = $null
    try { $actual = (Get-ItemProperty -LiteralPath $lp -Name $Nombre -ErrorAction Stop).$Nombre } catch { $actual = $null }
    if ($null -ne $actual -and "$actual" -eq "$Dato") { return $false }
    if (-not (Test-Path -LiteralPath $lp)) { New-Item -Path $lp -Force | Out-Null }
    $tipoPs = switch ($Tipo) { 'DWord' { 'DWord' } 'QWord' { 'QWord' } 'ExpandString' { 'ExpandString' } 'MultiString' { 'MultiString' } 'Binary' { 'Binary' } default { 'String' } }
    New-ItemProperty -LiteralPath $lp -Name $Nombre -PropertyType $tipoPs -Value $Dato -Force | Out-Null
    return $true
}

function Repair-AetherRegistro {
    param([string[]]$Raices)
    $repuestos = 0
    foreach ($archivo in Get-ChildItem -LiteralPath $tweaks -Recurse -Filter 'aether-*.psd1' -ErrorAction SilentlyContinue) {
        try { $def = Import-PowerShellDataFile -LiteralPath $archivo.FullName } catch { continue }
        if (-not $def.Registry) { continue }
        if ($def.Option -and -not (Test-OpcionElegida -Opcion $def.Option)) { continue }
        foreach ($r in $def.Registry) {
            if ($r.Action -and $r.Action -ne 'Set') { continue }
            $raiz = ($r.Path -split '\\')[0]
            if ($Raices -notcontains $raiz) { continue }
            if ($omitir -contains "$($r.Path)|$($r.Name)") { continue }
            try {
                if (Set-AetherValor -Ruta $r.Path -Nombre $r.Name -Tipo $r.Type -Dato $r.Data) {
                    $repuestos++
                    Write-Guardian "Repuesto: $($r.Path)\$($r.Name) = $($r.Data) ($($archivo.BaseName))"
                }
            }
            catch { Write-Guardian "No se pudo reponer $($r.Path)\$($r.Name): $($_.Exception.Message)" }
        }
    }
    return $repuestos
}

if ($Sistema) {
    # Red de trabajo completa (servicios, SMB, NetBIOS, red Privada, firewall, invitado, sin contraseña...).
    $red = Join-Path $tweaks 'networking\aether-red.ps1'
    if (Test-Path -LiteralPath $red) {
        try { & $red; Write-Guardian 'Red de trabajo revisada y aplicada.' }
        catch { Write-Guardian "Red: $($_.Exception.Message)" }
    }
    $n = Repair-AetherRegistro -Raices @('HKLM')
    Write-Guardian "Sistema: $n ajustes repuestos."
}
if ($Usuario) {
    $n = Repair-AetherRegistro -Raices @('HKCU', 'HKLM')
    Write-Guardian "Usuario $env:USERNAME: $n ajustes repuestos."
}
