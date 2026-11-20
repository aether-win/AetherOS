@{
    Name        = 'PowerSaving'
    Description = 'Power-saving. Disable applies the documented Aether AC power policy; Enable restores the prior power plan, or Balanced when none was saved. Both states use the same helper.'
    Elevation   = 'Admin'
    Script      = 'PowerSaving.ps1'
    States      = @(
        @{
            Name          = 'Disable'
            StateValue    = 0
            Launcher      = '3. Configuracion general\Ahorro de energia\Desactivar ahorro de energia.cmd'
            Reboot        = 'None'
            MachineAction = 'Invoke-AtlasPowerSavingToggle'
        }
        @{
            Name          = 'Enable'
            StateValue    = 1
            Launcher      = '3. Configuracion general\Ahorro de energia\Ahorro de energia normal (predeterminado).cmd'
            Reboot        = 'None'
            MachineAction = 'Invoke-AtlasPowerSavingToggle'
        }
    )
}
