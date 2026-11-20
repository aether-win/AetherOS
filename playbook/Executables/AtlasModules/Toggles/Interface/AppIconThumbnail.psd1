@{
    Name        = 'AppIconThumbnail'
    Description = 'Application icon overlays on File Explorer thumbnails.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '4. Interfaz\Explorador de archivos\Ícono de la app en miniaturas\Desactivar ícono de la app en miniaturas.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; Name = 'ShowTypeOverlay'; Type = 'DWord'; Data = 0 }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '4. Interfaz\Explorador de archivos\Ícono de la app en miniaturas\Activar ícono de la app en miniaturas (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; Name = 'ShowTypeOverlay'; Type = 'DWord'; Data = 1 }
            )
        }
    )
}
