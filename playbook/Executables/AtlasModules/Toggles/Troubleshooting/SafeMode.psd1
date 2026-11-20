@{
    Name          = 'SafeMode'
    Description   = 'Safe Mode boot configuration. Safe mode is a transient troubleshooting state, so it is never recorded: an upgrade replay must never boot the machine back into safe mode from a stale record.'
    Elevation     = 'Admin'
    NoStateRecord = $true
    Script        = 'SafeMode.ps1'
    States        = @(
        @{
            Name          = 'Minimal'
            Launcher      = '9. Solución de problemas\Modo seguro\Modo seguro.cmd'
            Reboot        = 'Recommend'
            MachineAction = 'Set-AtlasSafeModeBoot'
        }
        @{
            Name          = 'CommandPrompt'
            Launcher      = '9. Solución de problemas\Modo seguro\Modo seguro con símbolo del sistema.cmd'
            Reboot        = 'Recommend'
            MachineAction = 'Set-AtlasSafeModeBoot'
        }
        @{
            Name          = 'Networking'
            Launcher      = '9. Solución de problemas\Modo seguro\Modo seguro con red.cmd'
            Reboot        = 'Recommend'
            MachineAction = 'Set-AtlasSafeModeBoot'
        }
        @{
            Name          = 'Exit'
            Launcher      = '9. Solución de problemas\Modo seguro\Salir del modo seguro.cmd'
            Reboot        = 'Recommend'
            MachineAction = 'Set-AtlasSafeModeBoot'
        }
    )
}
