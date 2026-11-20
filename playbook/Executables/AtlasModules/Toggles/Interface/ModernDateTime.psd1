@{
    Name        = 'ModernDateTime'
    Description = 'Modern or old (Windows 8-style) date and time (tray clock) flyout.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '4. Interfaz\Paneles clásicos\Panel de fecha y hora\Panel de fecha y hora moderno (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell'; Name = 'UseWin32TrayClockExperience'; Type = 'DWord'; Data = 0 }
            )
        }
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '4. Interfaz\Paneles clásicos\Panel de fecha y hora\Panel de fecha y hora clásico.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell'; Name = 'UseWin32TrayClockExperience'; Type = 'DWord'; Data = 1 }
            )
        }
    )
}
