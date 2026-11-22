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

# --- Animación "está trabajando" (para que no parezca trabado) -----------------
# Un hilo aparte dibuja una línea en movimiento mientras una instalación (que no
# muestra nada) corre. Todo va en try/catch: si algo falla, la instalación sigue igual.
$script:SpinnerSync = $null
$script:SpinnerPS = $null
$script:SpinnerHandle = $null
function Start-AetherSpinner {
    param([string]$Texto = 'Trabajando')
    try {
        $sync = [hashtable]::Synchronized(@{ Activo = $true; Texto = $Texto })
        $ps = [PowerShell]::Create()
        [void]$ps.AddScript({
            param($sync)
            $frames = @('[>         ]', '[=>        ]', '[==>       ]', '[===>      ]', '[====>     ]',
                '[=====>    ]', '[======>   ]', '[=======>  ]', '[========> ]', '[=========>]')
            $i = 0
            while ($sync.Activo) {
                try { [Console]::Write(("`r  {0} {1}   " -f $sync.Texto, $frames[$i % $frames.Count])) } catch { $null = $_ }
                Start-Sleep -Milliseconds 150
                $i++
            }
            try { [Console]::Write("`r" + (' ' * 70) + "`r") } catch { $null = $_ }
        }).AddArgument($sync)
        $script:SpinnerSync = $sync
        $script:SpinnerPS = $ps
        $script:SpinnerHandle = $ps.BeginInvoke()
    }
    catch { $script:SpinnerSync = $null }
}
function Stop-AetherSpinner {
    try {
        if ($script:SpinnerSync) { $script:SpinnerSync.Activo = $false }
        Start-Sleep -Milliseconds 200
        if ($script:SpinnerPS) {
            try { $script:SpinnerPS.EndInvoke($script:SpinnerHandle) } catch { $null = $_ }
            $script:SpinnerPS.Dispose()
        }
    }
    catch { $null = $_ }
    finally { $script:SpinnerSync = $null; $script:SpinnerPS = $null; $script:SpinnerHandle = $null }
}

$apps = @()
try {
    $apps = @((Get-ItemProperty -Path 'HKLM:\SOFTWARE\AtlasOS\SetupOptions' -Name Apps -ErrorAction Stop).Apps)
}
catch { $apps = @() }
$hecho = (Get-ItemProperty -Path $claveAether -Name AppsInstaladas -ErrorAction SilentlyContinue).AppsInstaladas
if ($hecho -eq 1 -and -not $Forzar) { exit }
# Aunque no se haya elegido ninguna app, se sigue: los componentes para juegos y los
# drivers de mandos se revisan siempre (solo se instala lo que falte).

$registro = Join-Path $env:ProgramData 'Aether\apps.log'
New-Item -ItemType Directory -Path (Split-Path $registro) -Force | Out-Null
Start-Transcript -Path $registro -Append | Out-Null

Write-Host ''
Write-Host '  AETHER OS - Instalando tus apps y los componentes que falten' -ForegroundColor Cyan
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
            try { Add-AppxPackage -RegisterByFamilyName -MainPackage Microsoft.DesktopAppInstaller_8wekyb3d8bbwe -ErrorAction Stop } catch { Aviso 'No se pudo registrar el Instalador de aplicaciones; se sigue esperando.' }
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
    # Salida de winget al log (no a la consola) para que solo se vea la animación.
    $logWinget = Join-Path $env:ProgramData 'Aether\winget.log'
    Start-AetherSpinner -Texto 'Instalando'
    try { & $winget @argumentos *>> $logWinget }
    finally { Stop-AetherSpinner }
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
    else {
        Fallo 'Claude no se pudo instalar con winget; se abre su página de descarga'
        Start-Process 'https://claude.ai/download'
        $pendientes.Add('Claude: termina la instalación desde la página que se abrió')
    }
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
    $windhawkOk = Install-ConWinget -Id 'RamenSoftware.Windhawk'
    if (-not $windhawkOk) {
        # Respaldo: en una PC nueva winget a veces no está listo. Se baja el instalador
        # oficial de Windhawk y se instala en silencio (NSIS /S).
        Aviso 'winget no instaló Windhawk; probando con el instalador oficial...'
        try {
            $setup = Join-Path $env:TEMP 'windhawk_setup.exe'
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
            Invoke-WebRequest -Uri 'https://ramensoftware.com/downloads/windhawk_setup.exe' -OutFile $setup -UseBasicParsing -ErrorAction Stop
            $p = Start-Process -FilePath $setup -ArgumentList '/S' -PassThru -Wait
            Start-Sleep -Seconds 3
            $windhawkOk = ($p.ExitCode -eq 0) -or (Test-Path -LiteralPath (Join-Path $env:ProgramFiles 'Windhawk\windhawk.exe'))
            Remove-Item -LiteralPath $setup -Force -ErrorAction SilentlyContinue
        }
        catch { Aviso "No se pudo descargar el instalador de Windhawk: $($_.Exception.Message)" }
    }
    if ($windhawkOk) {
        Ok 'Windhawk instalado'
        $paquete = Join-Path $carpetaAether 'Wind-Aether\Wind-Aether-mods.json'
        $copia = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Wind-Aether-mods.json'
        Copy-Item -LiteralPath $paquete -Destination $copia -Force -ErrorAction SilentlyContinue
        $cli = @($env:ProgramFiles, ${env:ProgramFiles(x86)}) |
            Where-Object { $_ -and $_.Trim() } |
            ForEach-Object { Join-Path $_ 'Windhawk\windhawk-cli.exe' } |
            Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
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

# --- Componentes que necesitan juegos y programas (solo lo que falte) ---------------
# Se detecta cada uno por separado y solo se instala lo que falte, desde los paquetes
# oficiales de Microsoft (winget). Después se vuelve a comprobar para confirmar.
function Test-AetherVcRedist {
    param([ValidateSet('x64', 'x86')][string]$Arquitectura)
    $ruta = if ($Arquitectura -eq 'x64') {
        'HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64'
    }
    else {
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x86'
    }
    $clave = Get-ItemProperty -LiteralPath $ruta -ErrorAction SilentlyContinue
    return ($null -ne $clave -and [int]$clave.Installed -eq 1 -and [int]$clave.Major -ge 14)
}

function Test-AetherDirectX {
    # DirectX 9/10/11 "heredado" (d3dx9_43, xinput1_3): lo piden muchos juegos viejos.
    $sistema = [Environment]::SystemDirectory
    $wow = Join-Path ([Environment]::GetFolderPath('Windows')) 'SysWOW64'
    return (Test-Path -LiteralPath (Join-Path $sistema 'd3dx9_43.dll')) -and
        (Test-Path -LiteralPath (Join-Path $sistema 'xinput1_3.dll')) -and
        (Test-Path -LiteralPath (Join-Path $wow 'd3dx9_43.dll'))
}

function Test-AetherDotNetEscritorio {
    $base = Join-Path $env:ProgramFiles 'dotnet\shared\Microsoft.WindowsDesktop.App'
    return [bool](Get-ChildItem -LiteralPath $base -Directory -Filter '8.*' -ErrorAction SilentlyContinue)
}

function Test-AetherNetFx48 {
    $v = Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue
    return ($null -ne $v -and [int]$v.Release -ge 528040)
}

function Test-AetherNetFx35 {
    $v = Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5' -ErrorAction SilentlyContinue
    return ($null -ne $v -and [int]$v.Install -eq 1)
}

Titulo 'Componentes para juegos y programas'
$componentes = @(
    @{ Nombre = 'Visual C++ 2015-2022 (64 bits)'; Prueba = { Test-AetherVcRedist -Arquitectura x64 }; Id = 'Microsoft.VCRedist.2015+.x64' },
    @{ Nombre = 'Visual C++ 2015-2022 (32 bits)'; Prueba = { Test-AetherVcRedist -Arquitectura x86 }; Id = 'Microsoft.VCRedist.2015+.x86' },
    @{ Nombre = 'DirectX (bibliotecas para juegos)'; Prueba = { Test-AetherDirectX }; Id = 'Microsoft.DirectX' },
    @{ Nombre = '.NET Desktop Runtime 8'; Prueba = { Test-AetherDotNetEscritorio }; Id = 'Microsoft.DotNet.DesktopRuntime.8' }
)
foreach ($c in $componentes) {
    if (& $c.Prueba) { Ok "$($c.Nombre): ya estaba instalado (se omite)"; continue }
    [void](Install-ConWinget -Id $c.Id)
    if (& $c.Prueba) { Ok "$($c.Nombre): instalado y verificado" }
    else { Fallo "$($c.Nombre): no se pudo instalar"; $pendientes.Add("$($c.Nombre) (winget install $($c.Id))") }
}
if (Test-AetherNetFx48) { Ok '.NET Framework 4.8: presente (viene con Windows)' }
else { Aviso '.NET Framework 4.8 no se detectó; Windows Update lo trae.'; $pendientes.Add('.NET Framework 4.8') }
if (Test-AetherNetFx35) { Ok '.NET Framework 3.5: ya estaba activado (se omite)' }
else {
    # Lo usan programas y juegos viejos. Es una función de Windows (no un descargable aparte).
    Start-AetherSpinner -Texto 'Activando .NET Framework 3.5'
    try { & dism.exe /Online /Enable-Feature /FeatureName:NetFx3 /All /NoRestart /Quiet *>> (Join-Path $env:ProgramData 'Aether\winget.log') }
    finally { Stop-AetherSpinner }
    if (Test-AetherNetFx35) { Ok '.NET Framework 3.5: activado y verificado' }
    else { Aviso '.NET Framework 3.5 no se pudo activar (necesita internet).'; $pendientes.Add('.NET Framework 3.5: Panel de control > Programas > Activar o desactivar características') }
}

# --- Mandos: ViGEmBus y HidHide (solo si faltan) -------------------------------------
# ViGEmBus crea el mando virtual (Xbox 360 / PS4) y HidHide oculta el mando real para que
# los juegos no vean dos. Son los drivers firmados de Nefarius (los mismos de DS4Windows).
function Test-AetherDriverFirmado {
    param([string]$Servicio)
    $driver = Get-CimInstance -ClassName Win32_SystemDriver -Filter "Name='$Servicio'" -ErrorAction SilentlyContinue
    if (-not $driver -or -not $driver.PathName) { return $false }
    $ruta = $driver.PathName -replace '^\\\?\?\\', '' -replace '^\\SystemRoot', $env:SystemRoot
    if (-not (Test-Path -LiteralPath $ruta)) { return $true }   # el servicio existe; ruta no estándar
    $firma = Get-AuthenticodeSignature -LiteralPath $ruta -ErrorAction SilentlyContinue
    return ($null -ne $firma -and $firma.Status -eq 'Valid')
}

Titulo 'Drivers de mandos'
foreach ($d in @(
        @{ Nombre = 'ViGEmBus (mando virtual)'; Servicio = 'ViGEmBus'; Id = 'Nefarius.ViGEmBus' },
        @{ Nombre = 'HidHide (oculta el mando real)'; Servicio = 'HidHide'; Id = 'Nefarius.HidHide' }
    )) {
    if (Get-Service -Name $d.Servicio -ErrorAction SilentlyContinue) {
        if (Test-AetherDriverFirmado -Servicio $d.Servicio) { Ok "$($d.Nombre): ya estaba instalado y firmado (se omite)" }
        else { Aviso "$($d.Nombre): instalado, pero no se pudo comprobar su firma digital" }
        continue
    }
    [void](Install-ConWinget -Id $d.Id)
    if (Get-Service -Name $d.Servicio -ErrorAction SilentlyContinue) {
        if (Test-AetherDriverFirmado -Servicio $d.Servicio) { Ok "$($d.Nombre): instalado, firmado y verificado" }
        else { Aviso "$($d.Nombre): instalado (no se pudo comprobar la firma)" }
    }
    else { Fallo "$($d.Nombre): no se pudo instalar"; $pendientes.Add("$($d.Nombre): Aether Toolbox > Control > Instalar") }
}

# Aether: los drivers de AMD se quitaron (no es algo estándar: no todas las PCs son AMD,
# y abrir una página de descarga no encaja en una instalación automática).

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
