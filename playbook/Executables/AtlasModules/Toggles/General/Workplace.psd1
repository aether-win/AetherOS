@{
    Name        = 'Workplace'
    Description = 'Workplace (Access work or school) settings page visibility. The page is opened interactively only.'
    Elevation   = 'Admin'
    Warning     = 'Hides or shows the Access work or school Settings page. While hidden, this PC cannot be joined to a work or school account from Settings.'
    Script      = 'Workplace.ps1'
    States      = @(
        @{
            Name          = 'Disable'
            StateValue    = 0
            Launcher      = '3. Configuracion general\Cuentas de trabajo o escuela\Desactivar cuentas de trabajo o escuela.cmd'
            Reboot        = 'None'
            MachineAction = 'Hide-AtlasWorkplaceSettingsPage'
        }
        @{
            Name          = 'Enable'
            StateValue    = 1
            Launcher      = '3. Configuracion general\Cuentas de trabajo o escuela\Activar cuentas de trabajo o escuela.cmd'
            Reboot        = 'None'
            MachineAction = 'Show-AtlasWorkplaceSettingsPage'
        }
    )
}
