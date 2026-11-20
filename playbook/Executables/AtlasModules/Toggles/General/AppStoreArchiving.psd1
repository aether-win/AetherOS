@{
    Name        = 'AppStoreArchiving'
    Description = 'Microsoft Store automatic app archiving.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '3. Configuración general\Archivado de apps de la Store\Desactivar archivado de apps (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Appx'; Name = 'AllowAutomaticAppArchiving'; Type = 'DWord'; Data = 0 }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '3. Configuración general\Archivado de apps de la Store\Activar archivado de apps.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Appx'; Name = 'AllowAutomaticAppArchiving'; Type = 'DWord'; Data = 1 }
            )
        }
    )
}
