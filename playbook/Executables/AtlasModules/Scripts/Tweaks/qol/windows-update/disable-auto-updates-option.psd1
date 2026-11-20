@{
    Name        = 'Pausar Windows Update hasta el año 3000'
    Description = 'Aether: con la opción auto-updates-disable pausa Windows Update hasta el año 3000 con el toggle PauseUpdates, en vez de apagar servicios, para que la Store, Xbox y winget sigan funcionando. Se quita la pausa con el toggle (o el botón "Actualizar Windows ahora" de la app).'
    Option      = 'auto-updates-disable'
    Toggle      = @(
        @{ Name = 'PauseUpdates'; State = 'Enable' }
    )
}
