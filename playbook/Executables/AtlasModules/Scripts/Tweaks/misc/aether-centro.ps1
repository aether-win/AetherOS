# Companion de aether-centro.psd1: deja listo el Centro Aether.
#  - Bandeja (automatizacion en segundo plano: modo bateria automatico, atajos, etc.):
#    corre al iniciar sesion, OCULTA, con un .vbs (sin ventana de PowerShell).
#  - Interfaz: si esta el Centro NATIVO (WPF, CentroAether.exe), los accesos directos
#    lo abren mediante una tarea programada con privilegios altos -> SIN pedir permisos (UAC)
#    y sin consola. Si no esta el exe nativo, se usa la ventana clasica como respaldo.
$ErrorActionPreference = 'Stop'

trap {
    if (Get-Command Write-AtlasLog -ErrorAction SilentlyContinue) {
        Write-AtlasLog -Level Warning -Message ("aether-centro: paso omitido por un error no critico: " + $_.Exception.Message) -ErrorRecord $_
    }
    continue
}

$carpeta = Join-Path ([Environment]::GetFolderPath('Windows')) 'AtlasModules\Aether\Centro'
$centro = Join-Path $carpeta 'Centro-Aether.ps1'
$icono = Join-Path $carpeta 'aether.ico'
$exeNativo = Join-Path $carpeta 'app\CentroAether.exe'
if (-not (Test-Path -LiteralPath $centro -PathType Leaf)) {
    throw "Falta el Centro Aether en '$centro'."
}
$powershell = Join-Path ([Environment]::SystemDirectory) 'WindowsPowerShell\v1.0\powershell.exe'
$wscript = Join-Path ([Environment]::SystemDirectory) 'wscript.exe'
$shell = New-Object -ComObject WScript.Shell

# --- Bandeja oculta (segundo plano) -----------------------------------------------------
$vbsBandeja = Join-Path $carpeta 'Centro-Aether.vbs'
[IO.File]::WriteAllText($vbsBandeja, @"
' Lanza la bandeja del Centro Aether sin ventana de PowerShell.
Dim shell
Set shell = CreateObject("WScript.Shell")
shell.Run "$powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File ""$centro"" -Bandeja", 0, False
"@, (New-Object Text.UTF8Encoding($false)))

$run = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'
if (-not (Test-Path -LiteralPath $run)) { New-Item -Path $run -Force | Out-Null }
Set-ItemProperty -LiteralPath $run -Name 'AetherBandeja' -Type String -Value "`"$wscript`" `"$vbsBandeja`""

# --- Interfaz: nativa (preferida) o clasica (respaldo) ----------------------------------
$usarNativo = Test-Path -LiteralPath $exeNativo -PathType Leaf
$accesoTarget = $wscript
$accesoArgs = ""
$accesoIcono = "$icono,0"

if ($usarNativo) {
    # Tarea programada que abre el Centro nativo con privilegios altos (sin UAC).
    $accion = New-ScheduledTaskAction -Execute $exeNativo -WorkingDirectory (Split-Path $exeNativo)
    $principal = New-ScheduledTaskPrincipal -GroupId 'S-1-5-32-545' -RunLevel Highest
    $ajustes = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -ExecutionTimeLimit ([TimeSpan]::Zero)
    $tarea = New-ScheduledTask -Action $accion -Principal $principal -Settings $ajustes
    Register-ScheduledTask -TaskName 'Centro' -TaskPath '\Aether\' -InputObject $tarea -Force | Out-Null

    # .vbs que dispara la tarea sin consola (schtasks es un programa de consola).
    $vbsAbrir = Join-Path $carpeta 'Abrir-Centro.vbs'
    [IO.File]::WriteAllText($vbsAbrir, @"
' Abre el Centro Aether nativo mediante su tarea programada (sin ventana ni UAC).
Dim shell
Set shell = CreateObject("WScript.Shell")
shell.Run "schtasks /run /tn ""\Aether\Centro""", 0, False
"@, (New-Object Text.UTF8Encoding($false)))
    $accesoArgs = "`"$vbsAbrir`""
    $accesoIcono = "$exeNativo,0"
}
else {
    # Respaldo: la ventana clasica (WPF) sin consola.
    $vbsAbrir = Join-Path $carpeta 'Abrir-Centro.vbs'
    [IO.File]::WriteAllText($vbsAbrir, @"
' Abre el Centro Aether clasico sin ventana de PowerShell.
Dim shell
Set shell = CreateObject("WScript.Shell")
shell.Run "$powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File ""$centro""", 0, False
"@, (New-Object Text.UTF8Encoding($false)))
    $accesoArgs = "`"$vbsAbrir`""
}

foreach ($destino in @(
        (Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'Centro Aether.lnk'),
        (Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'Centro Aether.lnk')
    )) {
    try {
        $acceso = $shell.CreateShortcut($destino)
        $acceso.TargetPath = $accesoTarget
        $acceso.Arguments = $accesoArgs
        $acceso.WorkingDirectory = $carpeta
        $acceso.IconLocation = $accesoIcono
        $acceso.Description = 'Centro Aether: modos, red, respaldo, limpieza y mas.'
        $acceso.Save()
    }
    catch {
        Write-AtlasLog -Level Warning -Message "Centro Aether: no se pudo crear '$destino': $($_.Exception.Message)" -ErrorRecord $_
    }
}

Write-AtlasLog -Message "Centro Aether listo (interfaz $(if ($usarNativo) { 'nativa' } else { 'clasica' }), bandeja oculta)."
