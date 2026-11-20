@{
    Name          = 'AdvancedBootOptions'
    Description   = 'Always go to the advanced boot options on each boot.'
    Elevation     = 'Admin'
    Menu          = $true
    Launcher      = '6. Configuracion avanzada\Arranque\Comportamiento\Abrir siempre opciones de arranque avanzadas.cmd'
    SilentDefault = 'Disable'
    Script        = 'AdvancedBootOptions.ps1'
    States        = @(
        @{
            Name          = 'Disable'
            StateValue    = 0
            MenuLabel     = 'Disable always going to the advanced boot options (default)'
            Reboot        = 'Recommend'
            MachineAction = 'Disable-AtlasAdvancedBootOptions'
        }
        @{
            Name          = 'Enable'
            StateValue    = 1
            MenuLabel     = 'Enable always going to the advanced boot options'
            Reboot        = 'Recommend'
            MachineAction = 'Enable-AtlasAdvancedBootOptions'
        }
    )
}
