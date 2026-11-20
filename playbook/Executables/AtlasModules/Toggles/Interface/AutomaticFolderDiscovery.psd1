@{
    Name        = 'AutomaticFolderDiscovery'
    Description = 'Automatic folder type discovery in File Explorer.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '4. Interfaz\Explorador de archivos\Deteccion automatica de carpetas\Desactivar deteccion automatica de carpetas (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags'; Operation = 'DeleteKey'; SkipVerification = 'Resets cached folder views; the following FolderType entry and Explorer recreate this key.' }
                @{ Path = 'HKCU:\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell'; Name = 'FolderType'; Type = 'String'; Data = 'NotSpecified' }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '4. Interfaz\Explorador de archivos\Deteccion automatica de carpetas\Activar deteccion automatica de carpetas.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKCU:\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders'; Operation = 'DeleteKey' }
            )
        }
    )
}
