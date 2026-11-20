@{
    Name        = 'Edge de Aether (Google y limpieza)'
    Description = 'Aether: Edge se queda. Deja Google como buscador para los perfiles nuevos de Edge (archivo initial_preferences) y limpia Edge con directivas que funcionan en PCs no administradas: sin primer arranque, sin arranque en segundo plano ni Startup Boost, sin asistente de compras ni recomendaciones, y con pestañas en suspensión.'
    Registry    = @(
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'HideFirstRunExperience'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'StartupBoostEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'BackgroundModeEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'EdgeShoppingAssistantEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'ShowRecommendationsEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'SpotlightExperiencesAndRecommendationsEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'PersonalizationReportingEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'SleepingTabsEnabled'; Type = 'DWord'; Data = 1 }
    )
    Script      = 'aether-edge.ps1'
}
