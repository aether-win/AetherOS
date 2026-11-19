@{
    Name        = 'Configure OEM Information'
    Description = 'Pone la información de Aether OS en Configuración > Sistema > Información'
    Registry    = @(
        @{ Path = 'HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\OEMInformation'; Name = 'Manufacturer'; Type = 'String'; Data = 'Aether OS' }
    )
    # Fills in the dynamic values (Atlas version/model) that cannot be expressed declaratively.
    Script      = 'config-oem-information.ps1'
}
