[Diagnostics.CodeAnalysis.SuppressMessageAttribute(
    'PSReviewUnusedParameter',
    '',
    Justification = 'The software-phase harness parameters are consumed through its isolated child-scope command stubs.'
)]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute(
    'PSAvoidOverwritingBuiltInCmdlets',
    '',
    Justification = 'The isolated software-phase harness shadows Import-Module only while executing the phase under test.'
)]
param()

BeforeAll {
    . (Join-Path $PSScriptRoot 'AtlasTestHost.ps1')
    $script:atlasModulesRoot = (Resolve-Path (Join-Path -Path $PSScriptRoot -ChildPath '..\playbook\Executables\AtlasModules')).Path
    $script:phasesRoot = Join-Path -Path $script:atlasModulesRoot -ChildPath 'Scripts\Install\Phases'

    function Get-AtlasPhaseAst {
        param([Parameter(Mandatory = $true)][string]$Path)

        $tokens = $null
        $errors = $null
        return [System.Management.Automation.Language.Parser]::ParseFile($Path, [ref]$tokens, [ref]$errors)
    }

    function Get-AtlasPrivilegeSwitch {
        param([Parameter(Mandatory = $true)]$Ast)

        $commands = $Ast.FindAll({
                param($node)
                $node -is [System.Management.Automation.Language.CommandAst] -and
                $node.GetCommandName() -eq 'Assert-AtlasPrivilege'
            }, $true)

        $switches = foreach ($command in $commands) {
            foreach ($element in $command.CommandElements) {
                if ($element -is [System.Management.Automation.Language.CommandParameterAst]) {
                    $element.ParameterName
                }
            }
        }

        return @($switches)
    }

    function Invoke-AtlasSoftwarePhaseForTest {
        [CmdletBinding()]
        param(
            [Parameter(Mandatory = $true)][string]$Path,
            [Parameter(Mandatory = $true)]$Context,
            [Parameter(Mandatory = $true)][hashtable]$Options,
            [Parameter(Mandatory = $true)][hashtable]$ComponentOutcomes,
            [Parameter(Mandatory = $true)]$Attempts,
            [Parameter(Mandatory = $true)]$Logs,
            [Parameter(Mandatory = $true)]$UserIntegrationState,
            [int]$UserIntegrationExitCode = 0
        )

        & {
            function Assert-AtlasPrivilege {
                [CmdletBinding()]
                param([switch]$TrustedInstaller)
                [void]$TrustedInstaller
            }

            function Import-Module {
                [CmdletBinding()]
                param([string]$Name, [switch]$Force)
                [void]$Name
                [void]$Force
            }

            function Get-AtlasContext {
                return $Context
            }

            function Test-AtlasOption {
                param([Parameter(Mandatory = $true)][string]$Name)
                return $Options.ContainsKey($Name) -and [bool]$Options[$Name]
            }

            function Install-AtlasSoftware {
                param([Parameter(Mandatory = $true)][string[]]$Component)
                $name = [string]$Component[0]
                [void]$Attempts.Add($name)
                if (-not $ComponentOutcomes.ContainsKey($name)) {
                    return $true
                }
                $outcome = $ComponentOutcomes[$name]
                if ($outcome -is [Exception]) {
                    throw $outcome
                }
                return [bool]$outcome
            }

            function Invoke-AtlasAsUser {
                param(
                    [Parameter(Mandatory = $true)][string]$FilePath,
                    [Parameter(Mandatory = $true)][string]$Arguments
                )
                [void]$FilePath
                [void]$Arguments
                $UserIntegrationState.Count++
                return $UserIntegrationExitCode
            }

            function Write-AtlasLog {
                param(
                    [string]$Level = 'Information',
                    [Parameter(Mandatory = $true)][string]$Message
                )
                [void]$Logs.Add([pscustomobject]@{ Level = $Level; Message = $Message })
            }

            . $Path
        }
    }
}

Describe 'Install phase scripts' {
    BeforeDiscovery {
        $phasesRoot = Join-Path -Path $PSScriptRoot -ChildPath '..\playbook\Executables\AtlasModules\Scripts\Install\Phases'
        $script:phaseFiles = Get-ChildItem -Path $phasesRoot -Filter 'Invoke-*Phase.ps1' -File
    }

    It '<Name> returns or throws without exiting the install dispatcher host' -ForEach (
        $phaseFiles | ForEach-Object { @{ Name = $_.Name; FullName = $_.FullName } }
    ) {
        $ast = Get-AtlasPhaseAst -Path $FullName
        $exitStatements = @($ast.FindAll({
                    param($node)
                    $node -is [System.Management.Automation.Language.ExitStatementAst]
                }, $true))

        $exitStatements | Should -BeNullOrEmpty `
            -Because "$Name must return or throw so Invoke-AtlasInstall can record its phase as complete or failed"
    }

    # Every phase runs as TrustedInstaller; user work goes through the exact-user
    # launcher inside it.
    It '<Name> asserts TrustedInstaller privilege' -ForEach (
        $phaseFiles | ForEach-Object { @{ Name = $_.Name; FullName = $_.FullName } }
    ) {
        Get-AtlasPrivilegeSwitch -Ast (Get-AtlasPhaseAst -Path $FullName) | Should -Contain 'TrustedInstaller'
    }
}

Describe 'Software phase outcome aggregation' {
    BeforeEach {
        $script:softwarePhasePath = Join-Path -Path $script:phasesRoot `
            -ChildPath 'Invoke-SoftwarePhase.ps1'
        $script:softwareAttempts = New-Object 'Collections.Generic.List[string]'
        $script:softwareLogs = New-Object 'Collections.Generic.List[object]'
        $script:userIntegrationState = [pscustomobject]@{ Count = 0 }
    }

    # Aether: la fase solo instala los runtimes de Visual C++ y DirectX en instalaciones nuevas.
    It 'installs only the runtimes on a fresh install and nothing on an upgrade' -ForEach @(
        @{ IsUpgrade = $false; Expected = @('VCRedist', 'DirectX') }
        @{ IsUpgrade = $true; Expected = @() }
    ) {
        $context = [pscustomobject]@{ IsUpgrade = $IsUpgrade; IsOobe = $true; InteractiveUserSid = $null }

        {
            Invoke-AtlasSoftwarePhaseForTest `
                -Path $script:softwarePhasePath `
                -Context $context `
                -Options @{} `
                -ComponentOutcomes @{} `
                -Attempts $script:softwareAttempts `
                -Logs $script:softwareLogs `
                -UserIntegrationState $script:userIntegrationState
        } | Should -Not -Throw

        (@($script:softwareAttempts) -join ',') | Should -BeExactly (@($Expected) -join ',')
        $script:softwareLogs.Count | Should -Be 0
    }

    It 'only warns when the legacy DirectX runtime fails' {
        $context = [pscustomobject]@{ IsUpgrade = $false; IsOobe = $true; InteractiveUserSid = $null }

        {
            Invoke-AtlasSoftwarePhaseForTest `
                -Path $script:softwarePhasePath `
                -Context $context `
                -Options @{} `
                -ComponentOutcomes @{ DirectX = [InvalidOperationException]::new('simulated DirectX failure') } `
                -Attempts $script:softwareAttempts `
                -Logs $script:softwareLogs `
                -UserIntegrationState $script:userIntegrationState
        } | Should -Not -Throw

        @($script:softwareAttempts) | Should -Be @('VCRedist', 'DirectX')
        $script:softwareLogs.Count | Should -Be 1
        $script:softwareLogs[0].Message | Should -Match 'Optional legacy DirectX runtime was not installed; continuing'
    }

    It 'throws one aggregate when the Visual C++ runtimes fail' {
        $context = [pscustomobject]@{ IsUpgrade = $false; IsOobe = $true; InteractiveUserSid = $null }

        $failure = $null
        try {
            Invoke-AtlasSoftwarePhaseForTest `
                -Path $script:softwarePhasePath `
                -Context $context `
                -Options @{} `
                -ComponentOutcomes @{ VCRedist = $false } `
                -Attempts $script:softwareAttempts `
                -Logs $script:softwareLogs `
                -UserIntegrationState $script:userIntegrationState
        }
        catch {
            $failure = $_
        }

        $failure | Should -Not -BeNullOrEmpty
        $failure.Exception.Message | Should -BeExactly 'Software phase failed for components: VCRedist.'
        @($script:softwareAttempts) | Should -Be @('VCRedist', 'DirectX')
    }
}

Describe 'Services phase feature defaults' {
    BeforeAll {
        $script:servicesPhasePath = Join-Path -Path $script:phasesRoot `
            -ChildPath 'Invoke-ServicesPhase.ps1'

        # Runs the phase with its module commands stubbed. Each stub records its call, and
        # the recorded-state table decides what the verification read-back returns. A
        # toggle listed in AppliedStates records that state when the phase applies it.
        function Invoke-AtlasServicesPhaseForTest {
            [CmdletBinding()]
            param(
                [Parameter(Mandatory = $true)][string]$Path,
                [Parameter(Mandatory = $true)]$Calls,
                [Parameter(Mandatory = $true)][hashtable]$RecordedStates,
                [Parameter(Mandatory = $true)][hashtable]$Definitions,
                [hashtable]$AppliedStates = @{},
                [switch]$IsRebase,
                [switch]$BackupFails
            )

            & {
                function Get-AtlasContext {
                    return [pscustomobject]@{ IsRebase = [bool]$IsRebase }
                }

                function Assert-AtlasPrivilege {
                    [CmdletBinding()]
                    param([switch]$TrustedInstaller)
                    if ($TrustedInstaller) { [void]$Calls.Add('Privilege:TrustedInstaller') }
                }

                function Import-Module {
                    [CmdletBinding()]
                    param([string]$Name, [switch]$Force)
                    [void]$Force
                    [void]$Calls.Add("Import:$Name")
                }

                function Export-AtlasServicesBackup {
                    param([Parameter(Mandatory = $true)][string]$FilePath)
                    [void]$Calls.Add("Backup:$FilePath")
                    if ($BackupFails) { throw 'simulated backup failure' }
                }

                function Invoke-AtlasToggleMachineState {
                    param(
                        [Parameter(Mandatory = $true)][string]$Name,
                        [Parameter(Mandatory = $true)][string]$State
                    )
                    [void]$Calls.Add("Toggle:$Name=$State")
                    if ($AppliedStates.ContainsKey($Name)) {
                        $RecordedStates[$Name] = $AppliedStates[$Name]
                    }
                }

                function Get-AtlasToggleDefinition {
                    param([Parameter(Mandatory = $true)][string]$Name)
                    return $Definitions[$Name]
                }

                function Get-AtlasToggleState {
                    param([Parameter(Mandatory = $true)][string]$Name)
                    if (-not $RecordedStates.ContainsKey($Name)) {
                        return $null
                    }
                    return [pscustomobject]@{ State = $RecordedStates[$Name] }
                }

                function Write-AtlasLog {
                    param(
                        [string]$Level = 'Information',
                        [Parameter(Mandatory = $true)][string]$Message
                    )
                    [void]$Level
                    [void]$Calls.Add("Log:$Message")
                }

                . $Path
            }
        }
    }

    BeforeEach {
        $script:servicesCalls = New-Object 'Collections.Generic.List[string]'
        $script:toggleDefinitions = @{
            FileSharing = @{ Name = 'FileSharing'; States = @{ Disable = @{ StateValue = 0 }; Enable = @{ StateValue = 1 } } }
            Location    = @{ Name = 'Location'; States = @{ Disable = @{ StateValue = 0 }; Enable = @{ StateValue = 1 } } }
            Indexing    = @{ Name = 'Indexing'; States = @{ Disable = @{ StateValue = 0 }; Minimal = @{ StateValue = 1 }; Enable = @{ StateValue = 2 } } }
        }
    }

    It 'asserts TrustedInstaller, then backs up services before applying the three defaults in order' {
        $recorded = @{ FileSharing = 1; Location = 0; Indexing = 2 }

        Invoke-AtlasServicesPhaseForTest -Path $script:servicesPhasePath `
            -Calls $script:servicesCalls -RecordedStates $recorded `
            -Definitions $script:toggleDefinitions

        $script:servicesCalls[0] | Should -BeExactly 'Privilege:TrustedInstaller'
        $imports = @($script:servicesCalls | Where-Object { $_ -like 'Import:*' })
        $imports.Count | Should -BeGreaterThan 0
        foreach ($import in $imports) {
            $import.Substring('Import:'.Length) | Should -Exist -Because 'the phase must find its modules in the installed layout'
        }
        $machineWork = @($script:servicesCalls | Where-Object { $_ -like 'Backup:*' -or $_ -like 'Toggle:*' })
        $machineWork[0] | Should -BeLike 'Backup:*\AtlasModules\Other\winServices.reg'
        @($machineWork | Select-Object -Skip 1) | Should -Be @(
            'Toggle:FileSharing=Enable', 'Toggle:Location=Disable', 'Toggle:Indexing=Enable'
        )
    }

    It 'makes no machine change when the services backup fails' {
        {
            Invoke-AtlasServicesPhaseForTest -Path $script:servicesPhasePath `
                -Calls $script:servicesCalls -RecordedStates @{} `
                -Definitions $script:toggleDefinitions -BackupFails
        } | Should -Throw '*simulated backup failure*'

        @($script:servicesCalls | Where-Object { $_ -like 'Toggle:*' }) | Should -BeNullOrEmpty `
            -Because 'the backup is the only way back to the original services'
    }

    It 'fails when a default does not record its expected state and stops before later defaults' -TestCases @(
        @{ Recorded = @{ FileSharing = 1; Location = 1; Indexing = 2 }; Failing = 'Location'; State = 'Disable' }
        @{ Recorded = @{ FileSharing = 1; Location = 0 }; Failing = 'Indexing'; State = 'Enable' }
    ) {
        {
            Invoke-AtlasServicesPhaseForTest -Path $script:servicesPhasePath `
                -Calls $script:servicesCalls -RecordedStates $Recorded `
                -Definitions $script:toggleDefinitions
        } | Should -Throw "*Services phase toggle '$Failing' did not record state '$State'*"

        $toggleCalls = @($script:servicesCalls | Where-Object { $_ -like 'Toggle:*' })
        $toggleCalls[-1] | Should -BeExactly "Toggle:$Failing=$State"
    }

    It 'keeps a recorded choice on a Rebase and applies only the defaults nothing recorded' {
        $recorded = @{ FileSharing = 1 }

        Invoke-AtlasServicesPhaseForTest -Path $script:servicesPhasePath `
            -Calls $script:servicesCalls -RecordedStates $recorded `
            -AppliedStates @{ Location = 0; Indexing = 2 } `
            -Definitions $script:toggleDefinitions -IsRebase

        $machineWork = @($script:servicesCalls | Where-Object { $_ -like 'Backup:*' -or $_ -like 'Toggle:*' })
        $machineWork[0] | Should -BeLike 'Backup:*\AtlasModules\Other\winServices.reg'
        @($machineWork | Select-Object -Skip 1) | Should -Be @(
            'Toggle:Location=Disable', 'Toggle:Indexing=Enable'
        )
        $recorded.FileSharing | Should -Be 1
        @($script:servicesCalls | Where-Object { $_ -like "Log:Kept the recorded 'FileSharing' choice*" }).Count |
            Should -Be 1
    }
}
