@{
    Name        = 'TakeOwnership'
    Description = 'The Take Ownership context menu entries. Each state is applied from its .reg file under Scripts\Registry\TakeOwnership.'
    Elevation   = 'Admin'
    Script      = 'TakeOwnership.ps1'
    States      = @(
        @{
            Name          = 'Disable'
            StateValue    = 0
            Launcher      = '4. Interfaz\Menus contextuales\Tomar posesion\Quitar Tomar posesion del menu (predeterminado).cmd'
            Reboot        = 'None'
            MachineAction = 'Import-AtlasTakeOwnershipContextMenu'
        }
        @{
            Name          = 'Enable'
            StateValue    = 1
            Launcher      = '4. Interfaz\Menus contextuales\Tomar posesion\Agregar Tomar posesion al menu.cmd'
            Reboot        = 'None'
            MachineAction = 'Import-AtlasTakeOwnershipContextMenu'
        }
    )
}
