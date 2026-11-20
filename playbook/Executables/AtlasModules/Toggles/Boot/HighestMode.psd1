@{
    Name          = 'HighestMode'
    Description   = 'Highest graphical mode for boot applications.'
    Elevation     = 'Admin'
    Menu          = $true
    Launcher      = '6. Configuracion avanzada\Arranque\Comportamiento\Resolucion mas alta.cmd'
    SilentDefault = 'Disable'
    Script        = 'HighestMode.ps1'
    States        = @(
        @{
            Name          = 'Disable'
            StateValue    = 0
            MenuLabel     = 'Disable (default)'
            Reboot        = 'Recommend'
            MachineAction = 'Disable-AtlasHighestMode'
        }
        @{
            Name          = 'Enable'
            StateValue    = 1
            MenuLabel     = 'Enable'
            Reboot        = 'Recommend'
            MachineAction = 'Enable-AtlasHighestMode'
        }
    )
}
