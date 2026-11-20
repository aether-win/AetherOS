@{
    Name        = 'Office sin bloqueo de macros ni avisos de otra PC (Aether)'
    Description = 'Aether: para que los proyectos de Excel, Word y PowerPoint que pasas desde otra PC no lleguen bloqueados. Desactiva el bloqueo de macros descargadas de Internet, habilita las macros VBA sin aviso y desactiva la Vista protegida para archivos de Internet, ubicaciones no seguras y adjuntos. Reduce la seguridad de Office a cambio de comodidad; aplícalo solo en un equipo de confianza.'
    Registry    = @(
        @{ Path = 'HKCU\Software\Policies\Microsoft\Office\16.0\Excel\Security'; Name = 'VBAWarnings'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKCU\Software\Policies\Microsoft\Office\16.0\Excel\Security'; Name = 'blockcontentexecutionfrominternet'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKCU\Software\Policies\Microsoft\Office\16.0\Excel\Security\ProtectedView'; Name = 'DisableInternetFilesInPV'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKCU\Software\Policies\Microsoft\Office\16.0\Excel\Security\ProtectedView'; Name = 'DisableUnsafeLocationsInPV'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKCU\Software\Policies\Microsoft\Office\16.0\Excel\Security\ProtectedView'; Name = 'DisableAttachmentsInPV'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKCU\Software\Policies\Microsoft\Office\16.0\Word\Security'; Name = 'VBAWarnings'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKCU\Software\Policies\Microsoft\Office\16.0\Word\Security'; Name = 'blockcontentexecutionfrominternet'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKCU\Software\Policies\Microsoft\Office\16.0\Word\Security\ProtectedView'; Name = 'DisableInternetFilesInPV'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKCU\Software\Policies\Microsoft\Office\16.0\Word\Security\ProtectedView'; Name = 'DisableUnsafeLocationsInPV'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKCU\Software\Policies\Microsoft\Office\16.0\PowerPoint\Security'; Name = 'VBAWarnings'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKCU\Software\Policies\Microsoft\Office\16.0\PowerPoint\Security'; Name = 'blockcontentexecutionfrominternet'; Type = 'DWord'; Data = 0 }
    )
}
