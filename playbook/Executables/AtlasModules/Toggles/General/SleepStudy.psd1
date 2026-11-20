@{
    Name        = 'SleepStudy'
    Description = 'Sleep Study diagnostic event logs and the Power Efficiency Diagnostics scheduled task.'
    Elevation   = 'Admin'
    Script      = 'SleepStudy.ps1'
    States      = @(
        @{
            Name           = 'Disable'
            StateValue     = 0
            Launcher       = '3. Configuración general\Sleep Study\Desactivar Sleep Study (predeterminado).cmd'
            Reboot         = 'None'
            ScheduledTasks = @(
                @{ Path = '\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem'; Operation = 'Disable' }
            )
            MachineAction  = 'Set-AtlasSleepStudyEventLogState'
        }
        @{
            Name           = 'Enable'
            StateValue     = 1
            Launcher       = '3. Configuración general\Sleep Study\Activar Sleep Study.cmd'
            Reboot         = 'None'
            ScheduledTasks = @(
                @{ Path = '\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem'; Operation = 'Enable' }
            )
            MachineAction  = 'Set-AtlasSleepStudyEventLogState'
        }
    )
}
