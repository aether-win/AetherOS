@{
    Name        = 'EdgeSwipe'
    Description = 'Edge Swipe gesture on touch screens.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '4. Interfaz\Deslizar desde el borde\Permitir deslizar desde el borde (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\EdgeUI'; Name = 'AllowEdgeSwipe'; Operation = 'Delete' }
            )
        }
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '4. Interfaz\Deslizar desde el borde\No permitir deslizar desde el borde.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\EdgeUI'; Name = 'AllowEdgeSwipe'; Type = 'DWord'; Data = 0 }
            )
        }
    )
}
