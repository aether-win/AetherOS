# Companion de aether-centro.psd1: deja listo Aether Toolbox (antes "Centro Aether").
#  - Bandeja (automatizacion en segundo plano: modo bateria automatico, atajos, etc.):
#    corre al iniciar sesion, OCULTA, con un .vbs (sin ventana de PowerShell).
#  - Interfaz: si esta Aether Toolbox (WPF, AetherToolbox.exe), los accesos directos
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
$exeNativo = Join-Path $carpeta 'app\AetherToolbox.exe'
if (-not (Test-Path -LiteralPath $centro -PathType Leaf)) {
    throw "Falta Aether Toolbox en '$centro'."
}
$powershell = Join-Path ([Environment]::SystemDirectory) 'WindowsPowerShell\v1.0\powershell.exe'
$wscript = Join-Path ([Environment]::SystemDirectory) 'wscript.exe'
$shell = New-Object -ComObject WScript.Shell

# --- Bandeja oculta (segundo plano) -----------------------------------------------------
$vbsBandeja = Join-Path $carpeta 'Centro-Aether.vbs'
[IO.File]::WriteAllText($vbsBandeja, @"
' Lanza el vigilante de Aether Toolbox sin ventana de PowerShell.
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

# Las tareas se registran SIEMPRE (no dependen de que el exe ya este copiado en este momento).
& {
    # Tarea programada que abre el Centro nativo con privilegios altos (sin UAC).
    $accion = New-ScheduledTaskAction -Execute $exeNativo -WorkingDirectory (Split-Path $exeNativo)
    $principal = New-ScheduledTaskPrincipal -GroupId 'S-1-5-32-545' -RunLevel Highest
    $ajustes = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -ExecutionTimeLimit ([TimeSpan]::Zero)
    $tarea = New-ScheduledTask -Action $accion -Principal $principal -Settings $ajustes
    # Tareas viejas (nombre anterior "Centro"): se quitan para no dejar duplicados.
    foreach ($vieja in @('Centro', 'Centro segundo plano')) {
        Unregister-ScheduledTask -TaskName $vieja -TaskPath '\Aether\' -Confirm:$false -ErrorAction SilentlyContinue
    }
    Register-ScheduledTask -TaskName 'Toolbox' -TaskPath '\Aether\' -InputObject $tarea -Force | Out-Null

    # Segundo plano SIEMPRE activo: al iniciar sesion, el Centro arranca solo en la bandeja
    # (menu de modos y mando, atajos Ctrl+Alt+T/G/B), con privilegios altos y sin UAC.
    $accionFondo = New-ScheduledTaskAction -Execute $exeNativo -Argument '--segundo-plano' -WorkingDirectory (Split-Path $exeNativo)
    $disparador = New-ScheduledTaskTrigger -AtLogOn
    $tareaFondo = New-ScheduledTask -Action $accionFondo -Principal $principal -Settings $ajustes -Trigger $disparador
    Register-ScheduledTask -TaskName 'Toolbox segundo plano' -TaskPath '\Aether\' -InputObject $tareaFondo -Force | Out-Null

}

# --- Lanzador unico: decide al abrir, no al instalar --------------------------------------
# Si AetherToolbox.exe existe se abre SIEMPRE el nuevo (tarea sin UAC; si la tarea falta,
# el exe directo). La ventana clasica solo si el exe no esta.
$vbsAbrir = Join-Path $carpeta 'Abrir-Centro.vbs'
[IO.File]::WriteAllText($vbsAbrir, @"
' Abre Aether Toolbox (nuevo) o, si no esta, la version clasica.
Dim shell, fso, rc
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
If fso.FileExists("$exeNativo") Then
    rc = shell.Run("schtasks /run /tn ""\Aether\Toolbox""", 0, True)
    If rc <> 0 Then shell.Run """$exeNativo""", 1, False
Else
    shell.Run "$powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File ""$centro""", 0, False
End If
"@, (New-Object Text.UTF8Encoding($false)))
$accesoArgs = "`"$vbsAbrir`""
if ($usarNativo) {
    # El acceso directo apunta al ejecutable REAL de Aether Toolbox. El propio exe se
    # relanza con privilegios mediante la tarea \Aether\Toolbox (sin UAC).
    $accesoTarget = $exeNativo
    $accesoArgs = ''
    $accesoIcono = "$exeNativo,0"
}

# Accesos viejos con el nombre anterior ("Centro Aether").
foreach ($viejo in @(
        (Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'Centro Aether.lnk'),
        (Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'Centro Aether.lnk')
    )) {
    Remove-Item -LiteralPath $viejo -Force -ErrorAction SilentlyContinue
}

foreach ($destino in @(
        (Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'Aether Toolbox.lnk'),
        (Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'Aether Toolbox.lnk')
    )) {
    try {
        $acceso = $shell.CreateShortcut($destino)
        $acceso.TargetPath = $accesoTarget
        $acceso.Arguments = $accesoArgs
        $acceso.WorkingDirectory = $(if ($usarNativo) { Split-Path $exeNativo } else { $carpeta })
        $acceso.IconLocation = $accesoIcono
        $acceso.Description = 'Aether Toolbox: modos, mandos, red, respaldo, limpieza y mas.'
        $acceso.Save()
    }
    catch {
        Write-AtlasLog -Level Warning -Message "Aether Toolbox: no se pudo crear '$destino': $($_.Exception.Message)" -ErrorRecord $_
    }
}

# --- Aether Transfer (cliente FTP para la PS4 y servidores) -----------------------------
$transfer = Join-Path ([Environment]::GetFolderPath('Windows')) 'AtlasModules\Aether\Transfer\AetherTransfer.exe'
if (Test-Path -LiteralPath $transfer -PathType Leaf) {
    foreach ($destino in @(
            (Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'Aether Transfer.lnk'),
            (Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'Aether Transfer.lnk')
        )) {
        try {
            $acceso = $shell.CreateShortcut($destino)
            $acceso.TargetPath = $transfer
            $acceso.WorkingDirectory = Split-Path $transfer
            $acceso.IconLocation = "$transfer,0"
            $acceso.Description = 'Aether Transfer: cliente FTP/FTPS/SFTP (PS4 y servidores).'
            $acceso.Save()
        }
        catch {
            Write-AtlasLog -Level Warning -Message "Aether Transfer: no se pudo crear '$destino': $($_.Exception.Message)" -ErrorRecord $_
        }
    }
    Write-AtlasLog -Message 'Aether Transfer listo (accesos directos creados).'
}
else {
    Write-AtlasLog -Level Warning -Message "Aether Transfer no está en el paquete ('$transfer')."
}

Write-AtlasLog -Message "Aether Toolbox listo (interfaz $(if ($usarNativo) { 'nativa' } else { 'clasica' }), bandeja oculta)."
