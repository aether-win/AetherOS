# Companion of set-profile-pictures.psd1: renders the Atlas default account picture
# (AtlasModules\Other\user.png) at every size Windows expects.
#
# Aether: antes de sobrescribir, respalda las fotos predeterminadas ORIGINALES de Windows
# y guarda una copia de las de Aether. Una tarea al iniciar sesion decide cual usar: si el
# usuario tiene cuenta de Microsoft, restaura las originales (foto normal); si es cuenta
# local, deja la de Aether. Esto es cosmetico y nunca debe detener la instalacion.
$ErrorActionPreference = 'Stop'
trap {
    if (Get-Command Write-AtlasLog -ErrorAction SilentlyContinue) {
        Write-AtlasLog -Level Warning -Message ("Foto de perfil: paso omitido: " + $_.Exception.Message) -ErrorRecord $_
    }
    continue
}

$destino = "$([Environment]::GetFolderPath('CommonApplicationData'))\Microsoft\User Account Pictures"
$dirOriginales = "$([Environment]::GetFolderPath('CommonApplicationData'))\Aether\FotosPerfilOriginales"
$dirAether = "$([Environment]::GetFolderPath('CommonApplicationData'))\Aether\FotosPerfilAether"

# Respaldo de las fotos ORIGINALES (solo la primera vez, antes de sobrescribir).
if (-not (Test-Path -LiteralPath $dirOriginales)) {
    New-Item -ItemType Directory -Path $dirOriginales -Force | Out-Null
    Get-ChildItem -LiteralPath $destino -File -ErrorAction SilentlyContinue | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $dirOriginales $_.Name) -Force -ErrorAction SilentlyContinue
    }
}

Add-Type -AssemblyName System.Drawing
$userPng = Join-Path -Path ([Environment]::GetFolderPath('Windows')) -ChildPath 'AtlasModules\Other\user.png'
$img = [System.Drawing.Image]::FromFile((Get-Item -LiteralPath $userPng))

$resolutions = @{
    "user.png" = 448
    "user.bmp" = 448
    "guest.png" = 448
    "guest.bmp" = 448
    "user-192.png" = 192
    "user-48.png" = 48
    "user-40.png" = 40
    "user-32.png" = 32
}

# Set default profile pictures
foreach ($image in $resolutions.Keys) {
    $resolution = $resolutions[$image]

    $a = New-Object System.Drawing.Bitmap($resolution, $resolution)
    $graph = [System.Drawing.Graphics]::FromImage($a)
    $graph.DrawImage($img, 0, 0, $resolution, $resolution)
    $a.Save("$destino\$image")
}

# Guarda una copia de las fotos de Aether para poder re-aplicarlas en cuentas locales.
if (-not (Test-Path -LiteralPath $dirAether)) { New-Item -ItemType Directory -Path $dirAether -Force | Out-Null }
Get-ChildItem -LiteralPath $destino -File -ErrorAction SilentlyContinue | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $dirAether $_.Name) -Force -ErrorAction SilentlyContinue
}

# Tarea al iniciar sesion: elige la foto normal (cuenta Microsoft) o la de Aether (cuenta local).
$script = Join-Path ([Environment]::GetFolderPath('Windows')) 'AtlasModules\Aether\Foto-Perfil.ps1'
if (Test-Path -LiteralPath $script) {
    $accion = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$script`""
    $disparador = New-ScheduledTaskTrigger -AtLogOn
    $principal = New-ScheduledTaskPrincipal -GroupId 'S-1-5-32-545' -RunLevel Highest
    $ajustes = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
    $tarea = New-ScheduledTask -Action $accion -Trigger $disparador -Principal $principal -Settings $ajustes
    Register-ScheduledTask -TaskName 'Foto de perfil' -TaskPath '\Aether\' -InputObject $tarea -Force | Out-Null
}
