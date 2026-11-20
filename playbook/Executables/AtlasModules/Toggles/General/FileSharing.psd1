@{
    Name        = 'FileSharing'
    Description = 'File Sharing (network discovery, SMB, NetBIOS bindings) through the same Operations helpers the fresh install uses; Enable optionally adds Network to the Explorer navigation pane.'
    Elevation   = 'Admin'
    Script      = 'FileSharing.ps1'
    States      = @(
        @{
            Name          = 'Disable'
            StateValue    = 0
            Launcher      = '3. Configuración general\Uso compartido\Desactivar uso compartido.cmd'
            Reboot        = 'Prompt'
            MachineAction = 'Set-AtlasFileSharingMachineState'
        }
        @{
            Name          = 'Enable'
            StateValue    = 1
            Launcher      = '3. Configuración general\Uso compartido\Activar uso compartido (predeterminado).cmd'
            Reboot        = 'Prompt'
            MachineAction = 'Set-AtlasFileSharingMachineState'
            UserAction    = 'Add-AtlasFileSharingNetworkNavigationPane'
        }
    )
}
