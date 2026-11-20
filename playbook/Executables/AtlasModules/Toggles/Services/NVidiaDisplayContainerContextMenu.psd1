@{
    Name        = 'NVidiaDisplayContainerContextMenu'
    Description = 'Desktop context menu that enables or disables the NVIDIA Display Container LS service.'
    Elevation   = 'Admin'
    Warning     = 'Adds or removes a desktop context menu that stops or starts the NVIDIA Display Container LS service. Stopping it disables the NVIDIA Control Panel and most NVIDIA driver features.'
    Script      = 'NVidiaDisplayContainerContextMenu.ps1'
    States      = @(
        @{
            Name          = 'Enable'
            StateValue    = 1
            Launcher      = '6. Configuracion avanzada\Servicios\NVIDIA Display Container\Menu contextual\Agregar Container al menu.cmd'
            Reboot        = 'RestartExplorer'
            MachineAction = 'Add-AtlasNVidiaContainerContextMenu'
            ReplayApplicable = 'Test-AtlasNVidiaContainerContextMenuApplicable'
        }
        @{
            Name          = 'Disable'
            StateValue    = 0
            Launcher      = '6. Configuracion avanzada\Servicios\NVIDIA Display Container\Menu contextual\Quitar Container del menu (predeterminado).cmd'
            Reboot        = 'RestartExplorer'
            Registry      = @(
                @{ Path = 'HKLM:\SOFTWARE\Classes\DesktopBackground\Shell\NVIDIAContainer'; Operation = 'DeleteKey' }
            )
        }
    )
}
