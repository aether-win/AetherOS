@{
    Name        = 'ContextMenuTerminals'
    Description = 'The Terminals context menu: removed, full, or without Windows Terminal. Each state is applied from its .reg file under Scripts\Registry\Terminals.'
    Elevation   = 'Admin'
    Script      = 'ContextMenuTerminals.ps1'
    States      = @(
        @{
            Name          = 'Remove'
            StateValue    = 0
            Launcher      = '4. Interfaz\Menús contextuales\Terminales\Quitar terminales del menú (predeterminado).cmd'
            Reboot        = 'None'
            MachineAction = 'Import-AtlasTerminalsContextMenu'
        }
        @{
            Name          = 'Add'
            StateValue    = 1
            Launcher      = '4. Interfaz\Menús contextuales\Terminales\Agregar terminales.cmd'
            Reboot        = 'None'
            MachineAction = 'Import-AtlasTerminalsContextMenu'
        }
        @{
            Name          = 'AddNoWindowsTerminal'
            StateValue    = 2
            Launcher      = '4. Interfaz\Menús contextuales\Terminales\Agregar terminales (sin Windows Terminal).cmd'
            Reboot        = 'None'
            MachineAction = 'Import-AtlasTerminalsContextMenu'
        }
    )
}
