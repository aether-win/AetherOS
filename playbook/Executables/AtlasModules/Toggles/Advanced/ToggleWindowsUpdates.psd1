@{
    Name        = 'ToggleWindowsUpdates'
    Description = 'Windows Update services, scheduled tasks and policies through one menu launcher. Replay resolves the recorded state, so no SilentDefault is needed.'
    Elevation   = 'TrustedInstaller'
    Menu        = $true
    Launcher    = '6. Configuracion avanzada\Activar o desactivar Windows Update\Activar o desactivar Windows Update.cmd'
    Script      = 'ToggleWindowsUpdates.ps1'
    States      = @(
        @{
            Name          = 'Disable'
            StateValue    = 0
            MenuLabel     = 'Disable Windows Updates'
            Reboot        = 'Prompt'
            MachineAction = 'Disable-AtlasWindowsUpdates'
        }
        @{
            Name          = 'Enable'
            StateValue    = 1
            MenuLabel     = 'Enable Windows Updates'
            Reboot        = 'Prompt'
            MachineAction = 'Enable-AtlasWindowsUpdates'
        }
    )
}
