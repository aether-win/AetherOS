# Companion de aether-apps.psd1: solo guarda qué apps se eligieron. La instalación la
# hace el usuario al iniciar sesión (Initialize-NewUser registra Install-AetherApps.ps1).
$ErrorActionPreference = 'Stop'

# Aether: red de seguridad. Este companion configura cosas de Aether durante la
# instalación; si algo inesperado falla, se registra un aviso y se continúa, nunca
# se aborta la instalación (regla de Aether: nada bloquea el acceso, solo avisa).
trap {
    try { Write-AtlasLog -Level Warning -Message ("aether-apps: paso omitido por un error no crítico: " + $_.Exception.Message) -ErrorRecord $_ } catch { }
    continue
}

$opciones = @('app-claude', 'app-chatgpt', 'app-python', 'app-keyvault', 'app-pcmanager',
    'app-steam', 'app-amd-drivers', 'wind-aether')
$elegidas = @($opciones | Where-Object { Test-AtlasOption -Name $_ })

$setupOptions = 'HKLM:\SOFTWARE\AtlasOS\SetupOptions'
if (-not (Test-Path -LiteralPath $setupOptions)) {
    New-Item -Path $setupOptions -Force | Out-Null
}
New-ItemProperty -LiteralPath $setupOptions -Name 'Apps' -Value ([string[]]$elegidas) -PropertyType MultiString -Force | Out-Null
Write-AtlasLog -Message "Apps de Aether elegidas: $(if ($elegidas.Count) { $elegidas -join ', ' } else { 'ninguna' })."
