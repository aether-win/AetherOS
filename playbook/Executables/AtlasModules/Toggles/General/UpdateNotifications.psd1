@{
    Name        = 'UpdateNotifications'
    Description = 'Windows Update restart notifications.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '3. Configuración general\Avisos de actualización\Desactivar avisos de actualización.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'; Name = 'SetAutoRestartNotificationDisable'; Type = 'DWord'; Data = 1 }
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'; Name = 'SetUpdateNotificationLevel'; Type = 'DWord'; Data = 1 }
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'; Name = 'UpdateNotificationLevel'; Type = 'DWord'; Data = 2 }
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings'; Name = 'RestartNotificationsAllowed2'; Type = 'DWord'; Data = 0 }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '3. Configuración general\Avisos de actualización\Activar avisos de actualización (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'; Name = 'SetAutoRestartNotificationDisable'; Operation = 'Delete' }
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings'; Name = 'RestartNotificationsAllowed2'; Operation = 'Delete' }
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'; Name = 'SetUpdateNotificationLevel'; Operation = 'Delete' }
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'; Name = 'UpdateNotificationLevel'; Operation = 'Delete' }
            )
        }
    )
}
