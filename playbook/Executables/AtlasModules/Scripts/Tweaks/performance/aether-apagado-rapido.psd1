@{
    Name        = 'Apagado, reinicio y encendido más rápidos (Aether)'
    Description = 'Aether: acorta la espera al detener servicios en el apagado y reinicio, quita el retraso de las apps de inicio al encender y evita limpiar el archivo de paginación al apagar. No fuerza el cierre de programas con trabajo sin guardar (no pierde datos), solo reduce las esperas.'
    Registry    = @(
        @{ Path = 'HKLM\SYSTEM\CurrentControlSet\Control'; Name = 'WaitToKillServiceTimeout'; Type = 'String'; Data = '2000' }
        @{ Path = 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Name = 'ClearPageFileAtShutdown'; Type = 'DWord'; Data = 0 }
        @{ Path = 'HKCU\Control Panel\Desktop'; Name = 'WaitToKillAppTimeout'; Type = 'String'; Data = '4000' }
        @{ Path = 'HKCU\Control Panel\Desktop'; Name = 'HungAppTimeout'; Type = 'String'; Data = '4000' }
        @{ Path = 'HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize'; Name = 'StartupDelayInMSec'; Type = 'DWord'; Data = 0 }
    )
}
