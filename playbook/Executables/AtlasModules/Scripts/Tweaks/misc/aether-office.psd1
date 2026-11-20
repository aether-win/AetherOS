@{
    Name        = 'Prioridad alta para Office (Aether)'
    Description = 'Aether: cualquier app de Office (Word, Excel, PowerPoint, Outlook, OneNote, Access, Publisher, Project, Visio) arranca con prioridad de CPU y de disco alta, para que respondan por encima del resto de programas. Usa Image File Execution Options (no RealTime, para no ahogar el sistema).'
    Registry    = @(
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\WINWORD.EXE\PerfOptions'; Name = 'CpuPriorityClass'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\WINWORD.EXE\PerfOptions'; Name = 'IoPriority'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\EXCEL.EXE\PerfOptions'; Name = 'CpuPriorityClass'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\EXCEL.EXE\PerfOptions'; Name = 'IoPriority'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\POWERPNT.EXE\PerfOptions'; Name = 'CpuPriorityClass'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\POWERPNT.EXE\PerfOptions'; Name = 'IoPriority'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\OUTLOOK.EXE\PerfOptions'; Name = 'CpuPriorityClass'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\OUTLOOK.EXE\PerfOptions'; Name = 'IoPriority'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\ONENOTE.EXE\PerfOptions'; Name = 'CpuPriorityClass'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\ONENOTE.EXE\PerfOptions'; Name = 'IoPriority'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MSACCESS.EXE\PerfOptions'; Name = 'CpuPriorityClass'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MSPUB.EXE\PerfOptions'; Name = 'CpuPriorityClass'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\WINPROJ.EXE\PerfOptions'; Name = 'CpuPriorityClass'; Type = 'DWord'; Data = 3 }
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\VISIO.EXE\PerfOptions'; Name = 'CpuPriorityClass'; Type = 'DWord'; Data = 3 }
    )
}
