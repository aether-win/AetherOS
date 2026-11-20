# Companion de aether-apps.psd1: solo guarda qué apps se eligieron. La instalación la
# hace el usuario al iniciar sesión (Initialize-NewUser registra Install-AetherApps.ps1).
$ErrorActionPreference = 'Stop'

$opciones = @('app-claude', 'app-chatgpt', 'app-python', 'app-keyvault', 'app-pcmanager',
    'app-steam', 'app-amd-drivers', 'wind-aether')
$elegidas = @($opciones | Where-Object { Test-AtlasOption -Name $_ })

$setupOptions = 'HKLM:\SOFTWARE\AtlasOS\SetupOptions'
if (-not (Test-Path -LiteralPath $setupOptions)) {
    New-Item -Path $setupOptions -Force | Out-Null
}
New-ItemProperty -LiteralPath $setupOptions -Name 'Apps' -Value ([string[]]$elegidas) -PropertyType MultiString -Force | Out-Null
Write-AtlasLog -Message "Apps de Aether elegidas: $(if ($elegidas.Count) { $elegidas -join ', ' } else { 'ninguna' })."
