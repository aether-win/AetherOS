@{
    Name        = 'SecurityHealthTray'
    Description = 'Windows Security (Defender) tray icon startup entry, applied from the included .reg files.'
    Elevation   = 'Admin'
    Script      = 'SecurityHealthTray.ps1'
    States      = @(
        @{
            Name          = 'Disable'
            StateValue    = 0
            Launcher      = '7. Seguridad\Defender\Icono de seguridad en la bandeja\Quitar icono de seguridad del inicio (predeterminado).cmd'
            Reboot        = 'None'
            MachineAction = 'Set-AtlasSecurityHealthTrayStartup'
        }
        @{
            Name          = 'Enable'
            StateValue    = 1
            Launcher      = '7. Seguridad\Defender\Icono de seguridad en la bandeja\Agregar icono de seguridad al inicio.cmd'
            Reboot        = 'None'
            MachineAction = 'Set-AtlasSecurityHealthTrayStartup'
        }
    )
}
