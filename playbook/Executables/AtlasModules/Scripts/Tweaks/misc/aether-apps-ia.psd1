@{
    Name        = 'Mejor rendimiento para Claude y ChatGPT (Aether)'
    Description = 'Aether: las apps de Claude y ChatGPT arrancan con prioridad de CPU por encima de lo normal para que respondan con más fluidez, sin llegar a ahogar al resto del sistema.'
    Registry    = @(
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\Claude.exe\PerfOptions'; Name = 'CpuPriorityClass'; Type = 'DWord'; Data = 6 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\ChatGPT.exe\PerfOptions'; Name = 'CpuPriorityClass'; Type = 'DWord'; Data = 6 }
    )
}
