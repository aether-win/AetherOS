<#
    Aether OS - Instalar las apps elegidas en el asistente.
    Corre una sola vez, al primer inicio de sesión después de instalar Aether, como el
    usuario (con permisos de administrador; nunca como SYSTEM), para que las apps de la
    Store y las de usuario queden en su cuenta. Las casillas las guarda la tarea
    misc\aether-apps del instalador en HKLM\SOFTWARE\AtlasOS\SetupOptions (valor Apps).
#>
[CmdletBinding()]
param([switch]$Forzar)

$ErrorActionPreference = 'Continue'
$claveAether = 'HKLM:\SOFTWARE\AtlasOS\Aether'
$carpetaAether = Join-Path ([Environment]::GetFolderPath('Windows')) 'AtlasModules\Aether'

# --- Elevación (un solo aviso de UAC) -----------------------------------------
$identidad = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $identidad.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $argumentos = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"")
    if ($Forzar) { $argumentos += '-Forzar' }
    Start-Process -FilePath 'powershell.exe' -ArgumentList $argumentos -Verb RunAs
    exit
}

$Host.UI.RawUI.WindowTitle = 'Aether OS - Instalando tus apps'
function Titulo($texto) { Write-Host ''; Write-Host "=== $texto ===" -ForegroundColor Cyan }
function Ok($texto) { Write-Host "  [OK]     $texto" -ForegroundColor Green }
function Aviso($texto) { Write-Host "  [AVISO]  $texto" -ForegroundColor Magenta }
function Fallo($texto) { Write-Host "  [ERROR]  $texto" -ForegroundColor Red }

$apps = @()
try {
    $apps = @((Get-ItemProperty -Path 'HKLM:\SOFTWARE\AtlasOS\SetupOptions' -Name Apps -ErrorAction Stop).Apps)
}
catch { }
$hecho = (Get-ItemProperty -Path $claveAether -Name AppsInstaladas -ErrorAction SilentlyContinue).AppsInstaladas
if ($hecho -eq 1 -and -not $Forzar) { exit }
if ($apps.Count -eq 0) {
    Write-Host '  No elegiste apps en el asistente.'
    exit
}

$registro = Join-Path $env:ProgramData 'Aether\apps.log'
New-Item -ItemType Directory -Path (Split-Path $registro) -Force | Out-Null
Start-Transcript -Path $registro -Append | Out-Null

Write-Host ''
Write-Host '  AETHER OS - Instalando las apps que elegiste' -ForegroundColor Cyan
Write-Host '  Puedes seguir usando la PC. No cierres esta ventana hasta que termine.'

$pendientes = New-Object System.Collections.Generic.List[string]

# --- winget --------------------------------------------------------------------
function Get-Winget {
    $cmd = Get-Command winget.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $ruta = Join-Path $env:LOCALAPPDATA 'Microsoft\WindowsApps\winget.exe'
    if (Test-Path -LiteralPath $ruta) { return $ruta }
    return $null
}
$winget = $null
for ($i = 0; $i -lt 12 -and -not $winget; $i++) {
    $winget = Get-Winget
    if (-not $winget) {
        if ($i -eq 0) {
            Aviso 'Esperando a que Windows prepare winget (Instalador de aplicaciones)...'
            try { Add-AppxPackage -RegisterByFamilyName -MainPackage Microsoft.DesktopAppInstaller_8wekyb3d8bbwe -ErrorAction Stop } catch { }
        }
        Start-Sleep -Seconds 10
    }
}
if (-not $winget) { Fallo 'winget no está disponible; las apps que lo usan quedarán pendientes.' }

function Install-ConWinget {
    param([string]$Id, [string]$Fuente = 'winget', [string[]]$Extra = @())
    if (-not $winget) { return $false }
    $argumentos = @('install', '--id', $Id, '--exact', '--source', $Fuente,
        '--accept-package-agreements', '--accept-source-agreements', '--disable-interactivity') + $Extra
    & $winget @argumentos
    # 0 = instalado; -1978335189 = ya estaba instalado y al día.
    return ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq -1978335189)
}

function Get-Archivo {
    param([string]$Url, [string]$Destino)
    try {
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $Url -OutFile $Destino -UseBasicParsing -ErrorAction Stop
        return $true
    }
    catch {
        Fallo "No se pudo descargar $Url : $($_.Exception.Message)"
        return $false
    }
}

function New-AccesoDirecto {
    param([string]$Ruta, [string]$Destino, [string]$Icono)
    $shell = New-Object -ComObject WScript.Shell
    $acceso = $shell.CreateShortcut($Ruta)
    $acceso.TargetPath = $Destino
    $acceso.WorkingDirectory = Split-Path $Destino
    if ($Icono) { $acceso.IconLocation = $Icono }
    $acceso.Save()
}

# --- Cada app ----------------------------------------------------------------------
if ($apps -contains 'app-claude') {
    Titulo 'Claude'
    if (Install-ConWinget -Id 'Anthropic.Claude') { Ok 'Claude instalado' }
    else { Fallo 'Claude no se pudo instalar'; $pendientes.Add('Claude: https://claude.ai/download') }
}

if ($apps -contains 'app-chatgpt') {
    Titulo 'ChatGPT'
    if (Install-ConWinget -Id '9NT1R1C2HH7J' -Fuente 'msstore') { Ok 'ChatGPT instalado desde la Store' }
    else {
        Aviso 'La Store no pudo instalarlo (¿sin cuenta en la Store?). Se abre la página oficial.'
        Start-Process 'https://chatgpt.com/download/'
        $pendientes.Add('ChatGPT: termina la instalación desde la página que se abrió')
    }
}

if ($apps -contains 'app-pcmanager') {
    Titulo 'Microsoft PC Manager'
    if (Install-ConWinget -Id '9PM860492SZD' -Fuente 'msstore') { Ok 'PC Manager instalado desde la Store' }
    elseif (Install-ConWinget -Id 'Microsoft.PCManager') { Ok 'PC Manager instalado' }
    else {
        Start-Process 'https://pcmanager.microsoft.com/'
        $pendientes.Add('PC Manager: descárgalo desde la página que se abrió')
    }
}

if ($apps -contains 'app-python') {
    Titulo 'Python 3.13.16'
    $version = '3.13.16'
    $opciones = @('/quiet', 'InstallAllUsers=1', 'PrependPath=1', 'Include_launcher=1',
        'InstallLauncherAllUsers=1', 'Include_pip=1', 'Include_test=0', 'AssociateFiles=1')
    $instalador = Join-Path $env:TEMP "python-$version-amd64.exe"
    $listo = $false
    if (Get-Archivo -Url "https://www.python.org/ftp/python/$version/python-$version-amd64.exe" -Destino $instalador) {
        $proceso = Start-Process -FilePath $instalador -ArgumentList $opciones -Wait -PassThru
        $listo = ($proceso.ExitCode -eq 0)
    }
    if (-not $listo) {
        $listo = Install-ConWinget -Id 'Python.Python.3.13' -Extra @('--version', $version, '--scope', 'machine', '--override', ($opciones -join ' '))
    }
    if ($listo) {
        Ok "Python $version instalado (PATH, py y pip)"
        # Rutas largas (más de 260 caracteres).
        Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name LongPathsEnabled -Value 1 -Type DWord
        Ok 'Rutas largas activadas'
        # Sin los alias de la Store que abren la Store al escribir "python".
        foreach ($alias in @('python.exe', 'python3.exe')) {
            $ruta = Join-Path $env:LOCALAPPDATA "Microsoft\WindowsApps\$alias"
            if (Test-Path -LiteralPath $ruta) { Remove-Item -LiteralPath $ruta -Force -ErrorAction SilentlyContinue }
        }
        Ok 'Alias de la Store para python quitados'
    }
    else { Fallo 'Python no se pudo instalar'; $pendientes.Add("Python $($version): https://www.python.org/downloads/") }
    Remove-Item -LiteralPath $instalador -Force -ErrorAction SilentlyContinue
}

if ($apps -contains 'app-keyvault') {
    Titulo 'KeyVault'
    $origen = Join-Path $carpetaAether 'KeyVault\KeyVault.exe'
    $carpeta = Join-Path $env:ProgramFiles 'KeyVault'
    $exe = Join-Path $carpeta 'KeyVault.exe'
    try {
        New-Item -ItemType Directory -Path $carpeta -Force | Out-Null
        Copy-Item -LiteralPath $origen -Destination $exe -Force -ErrorAction Stop
        New-AccesoDirecto -Ruta (Join-Path ([Environment]::GetFolderPath('Desktop')) 'KeyVault.lnk') -Destino $exe -Icono "$exe,0"
        New-AccesoDirecto -Ruta (Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'KeyVault.lnk') -Destino $exe -Icono "$exe,0"
        Ok 'KeyVault listo (Escritorio y menú Inicio)'
    }
    catch { Fallo "KeyVault: $($_.Exception.Message)"; $pendientes.Add('KeyVault') }
}

if ($apps -contains 'app-steam') {
    Titulo 'Steam'
    if (Install-ConWinget -Id 'Valve.Steam') { Ok 'Steam instalado' }
    else { Fallo 'Steam no se pudo instalar'; $pendientes.Add('Steam: https://store.steampowered.com/about/') }
}

if ($apps -contains 'wind-aether') {
    Titulo 'Wind-Aether (Windhawk)'
    if (Install-ConWinget -Id 'RamenSoftware.Windhawk') {
        Ok 'Windhawk instalado'
        $paquete = Join-Path $carpetaAether 'Wind-Aether\Wind-Aether-mods.json'
        $copia = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Wind-Aether-mods.json'
        Copy-Item -LiteralPath $paquete -Destination $copia -Force -ErrorAction SilentlyContinue
        $cli = @(
            (Join-Path $env:ProgramFiles 'Windhawk\windhawk-cli.exe'),
            (Join-Path ${env:ProgramFiles(x86)} 'Windhawk\windhawk-cli.exe')
        ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
        if ($cli) {
            & $cli data import $paquete --yes
            if ($LASTEXITCODE -eq 0) { Ok 'Mods de Wind-Aether importados (todos desactivados; actívalos desde la app de Aether)' }
            else { Aviso 'Algunos mods no se importaron'; $pendientes.Add("Wind-Aether: en Windhawk, Ajustes > Importar, elige $copia") }
        }
        else {
            Aviso 'Esta versión de Windhawk no trae la herramienta de línea de comandos (llega con Windhawk 2.0).'
            $pendientes.Add("Wind-Aether: en Windhawk, Ajustes > Importar, elige $copia")
        }
    }
    else { Fallo 'Windhawk no se pudo instalar'; $pendientes.Add('Wind-Aether: https://windhawk.net') }
}

if ($apps -contains 'app-amd-drivers') {
    Titulo 'Drivers AMD'
    Aviso 'AMD no publica sus drivers en winget. Se abre su página: elige "AMD Ryzen 7 7730U" o usa el detector automático.'
    Start-Process 'https://www.amd.com/es/support/download/drivers.html'
    $pendientes.Add('Drivers AMD: descarga Adrenalin desde la página que se abrió')
}

# --- Resumen -------------------------------------------------------------------------
Titulo 'Resumen'
if ($pendientes.Count -eq 0) {
    Ok 'Todas las apps quedaron instaladas.'
}
else {
    Aviso 'Quedó pendiente:'
    $pendientes | ForEach-Object { Write-Host "   - $_" }
}
if (-not (Test-Path -LiteralPath $claveAether)) { New-Item -Path $claveAether -Force | Out-Null }
Set-ItemProperty -Path $claveAether -Name AppsInstaladas -Value 1 -Type DWord
Write-Host ''
Write-Host "  Registro: $registro" -ForegroundColor Gray
Stop-Transcript | Out-Null
Read-Host '  Presiona Enter para cerrar'
