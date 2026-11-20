@{
    Name        = 'CompactView'
    Description = 'Compact view (reduced item spacing) in File Explorer.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '4. Interfaz\Explorador de archivos\Vista compacta\Desactivar vista compacta.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; Name = 'UseCompactMode'; Type = 'DWord'; Data = 0 }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '4. Interfaz\Explorador de archivos\Vista compacta\Activar vista compacta (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; Name = 'UseCompactMode'; Type = 'DWord'; Data = 1 }
            )
        }
    )
}
