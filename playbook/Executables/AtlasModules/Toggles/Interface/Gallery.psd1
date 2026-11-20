@{
    Name        = 'Gallery'
    Description = 'Gallery item in the File Explorer navigation pane.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '4. Interfaz\Explorador de archivos\Galería\Ocultar Galería (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}'; Name = 'System.IsPinnedToNameSpaceTree'; Type = 'DWord'; Data = 0 }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '4. Interfaz\Explorador de archivos\Galería\Mostrar Galería.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}'; Name = 'System.IsPinnedToNameSpaceTree'; Type = 'DWord'; Data = 1 }
            )
        }
    )
}
