@{
    Name        = 'Quitar SmartScreen (Windows y Edge)'
    Description = 'Aether: con la opción defender-disable también se apaga todo SmartScreen: el del Explorador (archivos descargados), el de las apps de la Store y el de Microsoft Edge (sitios y descargas, incluido el bloqueo de apps no deseadas). Con defender-enable no se aplica y SmartScreen queda como lo trae Windows.'
    Option      = 'defender-disable'
    Registry    = @(
        # Windows (Explorador): directiva "Configurar SmartScreen de Windows Defender".
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Windows\System'; Name = 'EnableSmartScreen'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer'; Name = 'SmartScreenEnabled'; Type = 'String'; Data = 'Off' }
        # Apps de la Store que abren contenido web.
        @{ Path = 'HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost'; Name = 'EnableWebContentEvaluation'; Type = 'DWord'; Data = 0 }
        # Microsoft Edge: sitios, descargas y apps no deseadas.
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'SmartScreenEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'SmartScreenPuaEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'SmartScreenForTrustedDownloadsEnabled'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKLM\SOFTWARE\Policies\Microsoft\Edge'; Name = 'SmartScreenDnsRequestsEnabled'; Type = 'DWord'; Data = 0 }
    )
}
