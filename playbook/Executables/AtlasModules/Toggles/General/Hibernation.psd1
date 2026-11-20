@{
    Name        = 'Hibernation'
    Description = 'Hibernation (powercfg /hibernate) and the Start flyout hibernate option.'
    Elevation   = 'Admin'
    Script      = 'Hibernation.ps1'
    States      = @(
        @{
            Name          = 'Disable'
            StateValue    = 0
            Launcher      = '3. Configuracion general\Hibernacion\Desactivar hibernacion.cmd'
            Reboot        = 'Prompt'
            Registry      = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings'; Name = 'ShowHibernateOption'; Type = 'DWord'; Data = 0 }
            )
            MachineAction = 'Set-AtlasHibernationPowerState'
        }
        @{
            Name          = 'Enable'
            StateValue    = 1
            Launcher      = '3. Configuracion general\Hibernacion\Activar hibernacion (predeterminado).cmd'
            Reboot        = 'None'
            Registry      = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings'; Name = 'ShowHibernateOption'; Type = 'DWord'; Data = 1 }
            )
            MachineAction = 'Set-AtlasHibernationPowerState'
        }
    )
}
