# Software phase.
# Installs machine-wide initial runtimes. Runs as
# TrustedInstaller; downloads happen here at install time.
# Every selected component is attempted. Optional app failures are warnings;
# other failures are reported together after the remaining installation attempts.

Assert-AtlasPrivilege -TrustedInstaller

$scriptsRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$modulesRoot = Join-Path -Path $scriptsRoot -ChildPath 'Modules'
Import-Module -Name (Join-Path $modulesRoot 'Atlas.Software\Atlas.Software.psd1') -Force -ErrorAction Stop

$context = Get-AtlasContext
$requestedComponents = New-Object 'Collections.Generic.List[string]'
$failedComponents = New-Object 'Collections.Generic.List[string]'

# Initial software: Visual C++ Runtimes and DirectX (fresh installs only).
# Aether: NanaZip ya no se instala (Windows 11 abre .zip, .rar y .7z por sí mismo).
# Las apps que el usuario marca en el asistente (Claude, ChatGPT, Python, Steam...)
# se instalan en una fase propia de Aether, como el usuario y no como SYSTEM.
if (-not $context.IsUpgrade) {
    foreach ($component in @('VCRedist', 'DirectX')) {
        $requestedComponents.Add($component)
    }
}

foreach ($component in $requestedComponents) {
    try {
        $componentInstalled = [bool](Install-AtlasSoftware -Component $component)
        if (-not $componentInstalled) {
            throw "The '$component' installer reported an unsuccessful outcome."
        }
    }
    catch {
        if ($component -ceq 'DirectX') {
            Write-AtlasLog -Level Warning -Message `
                "Optional legacy DirectX runtime was not installed; continuing: $($_.Exception.Message)"
            continue
        }
        if (-not $failedComponents.Contains($component)) {
            $failedComponents.Add($component)
        }
        Write-AtlasLog -Level Warning -Message `
            "Software phase component '$component' failed: $($_.Exception.Message)"
    }
}

if ($failedComponents.Count -gt 0) {
    throw "Software phase failed for components: $($failedComponents -join ', ')."
}
