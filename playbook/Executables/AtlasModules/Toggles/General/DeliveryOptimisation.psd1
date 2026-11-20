@{
    Name        = 'DeliveryOptimisation'
    Description = 'Delivery Optimization through the DODownloadMode policy.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '3. Configuración general\Optimización de distribución\Desactivar optimización de distribución (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization'; Name = 'DODownloadMode'; Type = 'DWord'; Data = 0 }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '3. Configuración general\Optimización de distribución\Activar optimización de distribución.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization'; Name = 'DODownloadMode'; Operation = 'Delete' }
            )
        }
    )
}
