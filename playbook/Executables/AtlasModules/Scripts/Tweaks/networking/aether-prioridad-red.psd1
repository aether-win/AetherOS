@{
    Name        = 'Prioridad total a la red y al Enlace móvil (Aether)'
    Description = 'Aether: quita la limitación de red de Windows (NetworkThrottlingIndex) para que Wi-Fi y Ethernet rindan al máximo en descargas, streaming y juego, y da prioridad de CPU y disco alta al Enlace móvil (PhoneExperienceHost) porque se usa mucho. SystemResponsiveness bajo para favorecer la red y el primer plano.'
    Registry    = @(
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile'; Name = 'NetworkThrottlingIndex'; Type = 'DWord'; Data = 4294967295 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile'; Name = 'SystemResponsiveness'; Type = 'DWord'; Data = 10 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\PhoneExperienceHost.exe\PerfOptions'; Name = 'CpuPriorityClass'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\PhoneExperienceHost.exe\PerfOptions'; Name = 'IoPriority'; Type = 'DWord'; Data = 3 }
    )
}
