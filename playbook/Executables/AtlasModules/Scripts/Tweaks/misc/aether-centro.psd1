@{
    Name        = 'Centro Aether'
    Description = 'Aether: deja lista la app de control (Centro Aether): accesos directos en el menú Inicio y el Escritorio, el ícono de la bandeja al iniciar sesión (modos, atajos Ctrl+Alt+T/G/B, Modo Batería automático) y permite que la resolución alta del temporizador del Modo Gaming aplique a los juegos.'
    Registry    = @(
        # Windows 11 limita la resolución del temporizador a cada proceso; con esto el
        # temporizador rápido que pide la bandeja en Modo Gaming vale para todo el sistema.
        @{ Path = 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel'; Name = 'GlobalTimerResolutionRequests'; Type = 'DWord'; Data = 1 }
    )
    Script      = 'aether-centro.ps1'
}
