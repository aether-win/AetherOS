@{
    Name        = 'NetworkNavigationPane'
    Description = 'Network item in the File Explorer navigation pane.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '3. Configuracion general\Uso compartido\Red en el Explorador\Ocultar Red en el Explorador.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\SOFTWARE\Classes\CLSID\{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}'; Name = 'System.IsPinnedToNameSpaceTree'; Type = 'DWord'; Data = 0 }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '3. Configuracion general\Uso compartido\Red en el Explorador\Mostrar Red en el Explorador (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\SOFTWARE\Classes\CLSID\{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}'; Name = 'System.IsPinnedToNameSpaceTree'; Operation = 'Delete' }
            )
        }
    )
}
