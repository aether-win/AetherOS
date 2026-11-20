@{
    Name          = 'FixMSStoreIssues'
    Description   = 'Fixes Microsoft Store issues by running StoreFixer. Records no state.'
    Elevation     = 'TrustedInstaller'
    NoStateRecord = $true
    Script        = 'FixMSStoreIssues.ps1'
    States        = @(
        @{
            Name          = 'Run'
            Launcher      = '9. Solucion de problemas\Arreglar problemas de la Microsoft Store.cmd'
            Reboot        = 'None'
            MachineAction = 'Invoke-AtlasStoreFixer'
        }
    )
}
