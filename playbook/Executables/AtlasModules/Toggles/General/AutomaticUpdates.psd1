@{
    Name        = 'AutomaticUpdates'
    Description = 'Automatic Updates through the WindowsUpdate AU AUOptions policy.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '3. Configuración general\Actualizaciones automáticas\Desactivar actualizaciones automáticas.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'; Name = 'AUOptions'; Type = 'DWord'; Data = 2 }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '3. Configuración general\Actualizaciones automáticas\Activar actualizaciones automáticas (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'; Name = 'AUOptions'; Operation = 'Delete' }
            )
        }
    )
}
