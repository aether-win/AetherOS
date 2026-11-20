@{
    Name        = 'ModernBatteryFlyout'
    Description = 'Modern or old (Windows 8-style) battery flyout.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '4. Interfaz\Paneles clásicos\Panel de batería\Panel de batería moderno (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell'; Name = 'UseWin32BatteryFlyout'; Type = 'DWord'; Data = 0 }
            )
        }
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '4. Interfaz\Paneles clásicos\Panel de batería\Panel de batería clásico.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell'; Name = 'UseWin32BatteryFlyout'; Type = 'DWord'; Data = 1 }
            )
        }
    )
}
