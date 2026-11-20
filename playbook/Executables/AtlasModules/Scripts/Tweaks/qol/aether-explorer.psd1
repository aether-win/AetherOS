@{
    Name        = 'Explorador de archivos más completo (Aether)'
    Description = 'Aether: deja el menú contextual clásico completo de Windows 10 (sin "Mostrar más opciones"), muestra la ruta completa en la barra de título, expande el panel de navegación hasta la carpeta abierta.'
    Registry    = @(
        @{ Path = 'HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32'; Name = ''; Type = 'String'; Data = '' }
        @{ Path = 'HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\CabinetState'; Name = 'FullPath'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; Name = 'NavPaneExpandToCurrentFolder'; Type = 'DWord'; Data = 1 }
        @{ Path = 'HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; Name = 'ShowStatusBar'; Type = 'DWord'; Data = 1 }
    )
}
