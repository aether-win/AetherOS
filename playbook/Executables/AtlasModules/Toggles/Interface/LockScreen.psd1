@{
    Name        = 'LockScreen'
    Description = 'Windows lock screen. Enable shows it and verifies the blocking policy values are gone; Disable hides it.'
    Elevation   = 'Admin'
    Script      = 'LockScreen.ps1'
    States      = @(
        @{
            Name          = 'Enable'
            StateValue    = 1
            Launcher      = '4. Interfaz\Pantalla de bloqueo\Mostrar pantalla de bloqueo (predeterminado).cmd'
            Reboot        = 'None'
            Registry      = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization'; Name = 'NoLockScreen'; Operation = 'Delete' }
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization'; Name = 'NoChangingLockScreen'; Operation = 'Delete' }
            )
            MachineAction = 'Assert-AtlasLockScreenPolicyRemoved'
        }
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '4. Interfaz\Pantalla de bloqueo\Ocultar pantalla de bloqueo.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization'; Name = 'NoLockScreen'; Type = 'DWord'; Data = 1 }
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization'; Name = 'NoChangingLockScreen'; Type = 'DWord'; Data = 1 }
            )
        }
    )
}
