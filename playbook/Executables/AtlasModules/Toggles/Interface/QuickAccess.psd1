@{
    Name        = 'QuickAccess'
    Description = 'Quick Access in File Explorer (HubMode hides it).'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '4. Interfaz\Explorador de archivos\Acceso rápido\Quitar Acceso rápido.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer'; Name = 'HubMode'; Type = 'DWord'; Data = 1 }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '4. Interfaz\Explorador de archivos\Acceso rápido\Mostrar Acceso rápido (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer'; Name = 'HubMode'; Operation = 'Delete' }
            )
        }
    )
}
