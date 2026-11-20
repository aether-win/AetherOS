@{
    Name        = 'ShortcutText'
    Description = 'The " - Shortcut" text suffix on newly created shortcuts.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '4. Interfaz\Texto de accesos directos\Quitar texto - Acceso directo (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates'; Name = 'ShortcutNameTemplate'; Type = 'String'; Data = '"%s.lnk"' }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '4. Interfaz\Texto de accesos directos\Restaurar texto - Acceso directo.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates'; Name = 'ShortcutNameTemplate'; Operation = 'Delete' }
            )
        }
    )
}
