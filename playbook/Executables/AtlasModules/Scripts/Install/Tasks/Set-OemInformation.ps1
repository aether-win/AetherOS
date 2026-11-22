$ErrorActionPreference = 'Stop'

$version = 'AtlasVersionUndefined'
$bootDescription = "Aether OS $version"
$bcdEditPath = [IO.Path]::Combine([Environment]::SystemDirectory, 'bcdedit.exe')

if (-not [IO.File]::Exists($bcdEditPath)) {
    throw "The inbox bcdedit executable is missing at '$bcdEditPath'."
}

Write-Output 'Setting boot entry name...'
& $bcdEditPath /set description $bootDescription | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "bcdedit.exe failed to set the boot entry description to '$bootDescription' with exit code $LASTEXITCODE."
}

Write-Output 'Setting other versioned OEM information...'
$reportedVersion = "Aether OS $version"
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\OEMInformation' `
    -Name 'Model' -Value $reportedVersion -Type String -Force -ErrorAction Stop
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' `
    -Name 'RegisteredOrganization' -Value $reportedVersion -Type String -Force -ErrorAction Stop

# Nombre registrado del producto (no cambia el nombre del equipo ni de la cuenta).
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' `
    -Name 'RegisteredOwner' -Value 'Aether' -Type String -Force -ErrorAction Stop
