@{
    Name          = 'InstallSoftware'
    Description   = 'Opens the Aether software picker. Records no state.'
    Elevation     = 'None'
    NoStateRecord = $true
    Script        = 'InstallSoftware.ps1'
    States        = @(
        @{
            Name     = 'Run'
            Launcher = '1. Programas\Instalar programas.cmd'
            Reboot   = 'None'
            Action   = 'Invoke-AtlasSoftwarePicker'
        }
    )
}
