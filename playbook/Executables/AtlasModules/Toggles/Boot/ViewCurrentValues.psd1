@{
    Name          = 'ViewCurrentValues'
    Description   = 'Prints the current boot configuration (bcdedit /enum {current}); records no state.'
    Elevation     = 'Admin'
    NoStateRecord = $true
    Script        = 'ViewCurrentValues.ps1'
    States        = @(
        @{
            Name            = 'View'
            Launcher        = '6. Configuración avanzada\Arranque\Ver valores actuales.cmd'
            Reboot          = 'None'
            MachineAction   = 'Show-AtlasBootValues'
        }
    )
}
