@{
    Name          = 'ConfigVBS'
    Description   = 'Shows the current Virtualization-Based Security configuration once; records no state.'
    Elevation     = 'None'
    NoStateRecord = $true
    Script        = 'ConfigVBS.ps1'
    States        = @(
        @{
            Name            = 'Run'
            Launcher        = '7. Seguridad\Aislamiento del núcleo (VBS)\Configuración actual.cmd'
            Reboot          = 'None'
            Action          = 'Show-AtlasVbsConfiguration'
        }
    )
}
