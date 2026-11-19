# Companion of config-pins.psd1.
$ErrorActionPreference = 'Stop'

# Aether: Edge es el navegador y no se puede quitar, así que se ancla primero.
$browser = 'Microsoft Edge'

# Aether: tema de Windows elegido en el asistente.
$theme = if (Test-AtlasOption -Name 'theme-light') { 'Light' } else { 'Dark' }

# Value read by Initialize-NewUser.ps1 for the installing user and users created later.
$setupOptions = 'HKLM:\SOFTWARE\AtlasOS\SetupOptions'
if (-not (Test-Path -LiteralPath $setupOptions)) {
    New-Item -Path $setupOptions -Force | Out-Null
}
New-ItemProperty -LiteralPath $setupOptions -Name 'browser' -Value $browser -PropertyType String -Force | Out-Null
New-ItemProperty -LiteralPath $setupOptions -Name 'theme' -Value $theme -PropertyType String -Force | Out-Null
