@{
    Name        = 'RunWithPriority'
    Description = 'The Run with priority cascading entry in the .exe context menu.'
    Elevation   = 'Admin'
    Script      = 'RunWithPriority.ps1'
    States      = @(
        @{
            Name          = 'Enable'
            StateValue    = 1
            Launcher      = '4. Interfaz\Menús contextuales\Ejecutar con prioridad\Agregar Ejecutar con prioridad al menú.cmd'
            Reboot        = 'None'
            MachineAction = 'Add-AtlasRunWithPriorityContextMenu'
        }
        @{
            Name       = 'Disable'
            StateValue = 0
            Launcher   = '4. Interfaz\Menús contextuales\Ejecutar con prioridad\Quitar Ejecutar con prioridad del menú (predeterminado).cmd'
            Reboot     = 'None'
            Registry   = @(
                @{ Path = 'HKLM:\SOFTWARE\Classes\exefile\Shell\Priority'; Operation = 'DeleteKey' }
            )
        }
    )
}
