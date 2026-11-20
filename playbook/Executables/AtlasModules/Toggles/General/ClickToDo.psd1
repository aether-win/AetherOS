@{
    Name        = 'ClickToDo'
    Description = 'Click To Do through the Windows AI DisableClickToDo policy.'
    Elevation   = 'Admin'
    States      = @(
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '3. Configuración general\Funciones de IA\Click To Do\Desactivar Click To Do (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI'; Name = 'DisableClickToDo'; Type = 'DWord'; Data = 1 }
            )
        }
        @{
            Name       = 'Enable'
            StateValue = 1
            Launcher   = '3. Configuración general\Funciones de IA\Click To Do\Activar Click To Do.cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI'; Name = 'DisableClickToDo'; Operation = 'Delete' }
            )
        }
    )
}
