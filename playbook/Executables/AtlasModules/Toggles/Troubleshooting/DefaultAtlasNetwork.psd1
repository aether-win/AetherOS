@{
    Name        = 'DefaultAtlasNetwork'
    Description = 'Network adapter reset. Enable applies Atlas defaults; Disable restores Windows defaults. Both states share the Atlas.Network Set-AtlasNetworkDefaults implementation.'
    Elevation   = 'Admin'
    NoStateRecord = $true
    Script      = 'DefaultAtlasNetwork.ps1'
    States      = @(
        @{
            Name            = 'Enable'
            StateValue      = 1
            Launcher        = '9. Solución de problemas\Red\Restablecer red a la de Aether.cmd'
            Reboot          = 'Recommend'
            MachineAction   = 'Set-AtlasNetworkDefaultState'
        }
        @{
            Name            = 'Disable'
            StateValue      = 0
            Launcher        = '9. Solución de problemas\Red\Restablecer red a la de Windows.cmd'
            Reboot          = 'Recommend'
            MachineAction   = 'Set-AtlasNetworkDefaultState'
        }
    )
}
