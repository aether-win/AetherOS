@{
    Name        = 'Edge de Aether (limpieza y arranque rápido)'
    Description = 'Aether: Edge se queda y abre más rápido (Startup Boost activado). Limpia Edge con directivas que funcionan en PCs no administradas: sin primer arranque, sin asistente de compras ni recomendaciones, y con pestañas en suspensión para ahorrar memoria.'
    Registry    = @(
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'HideFirstRunExperience'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'StartupBoostEnabled'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'EdgeShoppingAssistantEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'ShowRecommendationsEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'SpotlightExperiencesAndRecommendationsEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'PersonalizationReportingEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'SleepingTabsEnabled'; Type = 'DWord'; Data = 1 }
    )
}
