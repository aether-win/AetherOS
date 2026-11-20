@{
    Name        = 'NetworkDiscovery'
    Description = 'Network Discovery services. Enable first enables SMB; network profiles and firewall discovery rules remain separate choices in Advanced sharing settings. Explorer visibility is controlled by Network Navigation Pane.'
    Elevation   = 'Admin'
    Warning     = 'Changes the network discovery services. While disabled, this PC cannot find or be found by other devices on the network.'
    Script      = 'NetworkDiscovery.ps1'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '6. Configuración avanzada\Servicios\Detección de redes\Desactivar detección de redes.cmd'
            Reboot     = 'Recommend'
            Services   = @(
                @{ Name = 'fdPHost'; StartupType = 4 }
                @{ Name = 'FDResPub'; StartupType = 4 }
                @{ Name = 'lmhosts'; StartupType = 4 }
                @{ Name = 'SSDPSRV'; StartupType = 4 }
            )
        }
        @{
            Name          = 'Enable'
            StateValue    = 1
            Launcher      = '6. Configuración avanzada\Servicios\Detección de redes\Activar detección de redes (predeterminado).cmd'
            Reboot        = 'Recommend'
            MachineAction = 'Enable-AtlasNetworkDiscoveryServices'
        }
    )
}
