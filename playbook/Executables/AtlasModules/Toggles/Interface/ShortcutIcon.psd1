@{
    Name        = 'ShortcutIcon'
    Description = 'Shortcut overlay icon: the default arrow, the classic arrow or none.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Default'
            StateValue = 0
            Launcher   = '4. Interfaz\Flecha de accesos directos\La de Windows (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons'; Name = '29'; Operation = 'Delete' }
            )
        }
        @{
            Name       = 'Classic'
            StateValue = 1
            Launcher   = '4. Interfaz\Flecha de accesos directos\Clásica.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons'; Name = '29'; Type = 'ExpandString'; Data = '%SystemRoot%\AtlasModules\Other\Classic.ico,0' }
            )
        }
        @{
            Name       = 'None'
            StateValue = 2
            Launcher   = '4. Interfaz\Flecha de accesos directos\Sin flecha (riesgo de seguridad).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons'; Name = '29'; Type = 'ExpandString'; Data = '%SystemRoot%\AtlasModules\Other\Blank.ico,0' }
            )
        }
    )
}
