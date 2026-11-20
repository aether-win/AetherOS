@{
    Name          = 'KernelParameters'
    Description   = 'Editing of kernel parameters on startup.'
    Elevation     = 'Admin'
    Menu          = $true
    Launcher      = '6. Configuracion avanzada\Arranque\Comportamiento\Editar parametros del kernel al arrancar.cmd'
    SilentDefault = 'Disable'
    Script        = 'KernelParameters.ps1'
    States        = @(
        @{
            Name          = 'Disable'
            StateValue    = 0
            MenuLabel     = 'Disable editing of kernel parameters on startup (default)'
            Reboot        = 'Recommend'
            MachineAction = 'Disable-AtlasKernelParameters'
        }
        @{
            Name          = 'Enable'
            StateValue    = 1
            MenuLabel     = 'Enable editing of kernel parameters on startup'
            Reboot        = 'Recommend'
            MachineAction = 'Enable-AtlasKernelParameters'
        }
    )
}
