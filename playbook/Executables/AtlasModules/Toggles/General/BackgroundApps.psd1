@{
    Name        = 'BackgroundApps'
    Description = 'Per-user global background app access.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '3. Configuracion general\Apps en segundo plano\Desactivar apps en segundo plano.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications'; Name = 'GlobalUserDisabled'; Type = 'DWord'; Data = 1 }
                @{ Path = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Search'; Name = 'BackgroundAppGlobalToggle'; Type = 'DWord'; Data = 0; AllowOsProtected = $true }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '3. Configuracion general\Apps en segundo plano\Permitir apps en segundo plano (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications'; Name = 'GlobalUserDisabled'; Operation = 'Delete' }
                @{ Path = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Search'; Name = 'BackgroundAppGlobalToggle'; Operation = 'Delete'; AllowOsProtected = $true }
            )
        }
    )
}
