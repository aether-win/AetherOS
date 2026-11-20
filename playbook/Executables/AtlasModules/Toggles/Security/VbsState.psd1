@{
    Name        = 'VbsState'
    Description = 'Documented Virtualization-Based Security and memory-integrity configuration.'
    Elevation   = 'Admin'
    Script      = 'VbsState.ps1'
    States      = @(
        @{
            Name             = 'Disable'
            StateValue       = 0
            Launcher         = '7. Seguridad\Aislamiento del núcleo (VBS)\Desactivar VBS.cmd'
            Reboot           = 'Recommend'
            MachineAction    = 'Set-AtlasVbsState'
            ReplayApplicable = 'Test-AtlasVbsStateReplayApplicable'
        }
        @{
            Name             = 'Enable'
            StateValue       = 1
            Launcher         = '7. Seguridad\Aislamiento del núcleo (VBS)\Activar VBS.cmd'
            Reboot           = 'Recommend'
            MachineAction    = 'Set-AtlasVbsState'
            ReplayApplicable = 'Test-AtlasVbsStateReplayApplicable'
        }
    )
}
