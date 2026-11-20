@{
    Name        = 'Sleep'
    Description = 'Sleep power-scheme settings, with an optional hibernation follow-up on an interactive disable.'
    Elevation   = 'Admin'
    Script      = 'Sleep.ps1'
    States      = @(
        @{
            Name          = 'Disable'
            StateValue    = 0
            Launcher      = '3. Configuración general\Suspensión\Desactivar suspensión.cmd'
            Reboot        = 'None'
            MachineAction = 'Set-AtlasSleepState'
        }
        @{
            Name          = 'Enable'
            StateValue    = 1
            Launcher      = '3. Configuración general\Suspensión\Activar suspensión (predeterminado).cmd'
            Reboot        = 'None'
            MachineAction = 'Set-AtlasSleepState'
        }
    )
}
