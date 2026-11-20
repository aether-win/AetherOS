@{
    Name        = 'ModernVolumeFlyout'
    Description = 'Modern or old (Windows 8-style) volume flyout.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '4. Interfaz\Paneles clásicos\Panel de volumen\Panel de volumen moderno (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\MTCUVC'; Name = 'EnableMtcUvc'; Operation = 'Delete' }
            )
        }
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '4. Interfaz\Paneles clásicos\Panel de volumen\Panel de volumen clásico.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\MTCUVC'; Name = 'EnableMtcUvc'; Type = 'DWord'; Data = 0 }
            )
        }
    )
}
