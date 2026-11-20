@{
    Name        = 'ProcessExplorer'
    Description = 'Replaces Task Manager with Sysinternals Process Explorer, or restores Task Manager.'
    Elevation   = 'Admin'
    Script      = 'ProcessExplorer.ps1'
    States      = @(
        @{
            Name          = 'Enable'
            StateValue    = 1
            Launcher      = '6. Configuración avanzada\Process Explorer\Instalar Process Explorer.cmd'
            Reboot        = 'None'
            MachineAction = 'Install-AtlasProcessExplorer'
            UserAction    = 'Set-AtlasProcessExplorerUserPreference'
        }
        @{
            Name          = 'Disable'
            StateValue    = 0
            Launcher      = '6. Configuración avanzada\Process Explorer\Desinstalar Process Explorer.cmd'
            Reboot        = 'None'
            MachineAction = 'Uninstall-AtlasProcessExplorer'
        }
    )
}
