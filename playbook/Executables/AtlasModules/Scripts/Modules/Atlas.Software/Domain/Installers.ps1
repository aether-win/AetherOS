# Atlas.Software domain: install-time software downloads.

function Get-AtlasSoftwareComponentMap {
    # Component -> installer function. 'SevenZip' maps to the archive-tool installer,
    # which installs NanaZip unless 7-Zip is already installed. Keep the legacy
    # component name for callers.
    return @{
        SevenZip  = 'Install-AtlasArchiveTool'
        VCRedist  = 'Install-AtlasVisualCppRuntimes'
        DirectX   = 'Install-AtlasDirectXRuntime'
    }
}

function Test-AtlasSoftwareArm64 {
    [CmdletBinding()]
    param()

    $computerSystems = @(Get-CimInstance `
            -ClassName Win32_ComputerSystem `
            -ErrorAction Stop)
    if ($computerSystems.Count -ne 1) {
        throw "Expected exactly one Win32_ComputerSystem result, but received $($computerSystems.Count)."
    }

    $systemType = [string]$computerSystems[0].SystemType
    switch -CaseSensitive ($systemType) {
        'ARM64-based PC' { return $true }
        'x64-based PC' { return $false }
        default {
            throw "Unsupported or unreadable Win32_ComputerSystem SystemType '$systemType'."
        }
    }
}

function Invoke-AtlasSoftwareDownload {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][uri]$Uri,
        [Parameter(Mandatory = $true)][string]$Destination,
        [Parameter(Mandatory = $true)][string]$Description,
        [ValidateRange(1, 1GB)][long]$MaximumBytes = 1GB,
        [ValidateRange(1, 3600)][int]$MaximumSeconds = 900
    )

    if ($Uri.Scheme -cne 'https' -or -not [string]::IsNullOrEmpty($Uri.UserInfo)) {
        throw "Only HTTPS software download URIs without user information are allowed: '$Uri'."
    }
    if ([string]::IsNullOrWhiteSpace($Destination) -or
        $Destination.IndexOf([char]0) -ge 0 -or
        $Destination -cnotmatch '^[A-Za-z]:[\\/]' -or
        [IO.Path]::GetFullPath($Destination).Substring(2).Contains(':')) {
        throw 'A software download destination must be an explicit local path without an alternate data stream.'
    }

    $destinationPath = [IO.Path]::GetFullPath($Destination)
    if ([IO.File]::Exists($destinationPath) -or [IO.Directory]::Exists($destinationPath)) {
        throw "The software download destination '$destinationPath' already exists."
    }
    $destinationParent = [IO.Path]::GetDirectoryName($destinationPath)
    $resolvedParent = Resolve-AtlasProtectedExecutionPath `
        -Path $destinationParent `
        -PathType Container `
        -Description 'The software download directory'
    if (-not $resolvedParent.Equals($destinationParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "The software download directory '$destinationParent' did not resolve to its exact path."
    }

    Write-Host "Downloading $Description..."
    try {
        $stream = [IO.File]::Open(
            $destinationPath,
            [IO.FileMode]::CreateNew,
            [IO.FileAccess]::Write,
            [IO.FileShare]::None
        )
        try {
            $bytes = Invoke-AtlasBoundedHttpGet `
                -Uri $Uri `
                -OutputStream $stream `
                -MaximumBytes $MaximumBytes `
                -MaximumSeconds $MaximumSeconds `
                -AllowRedirect
        }
        finally {
            $stream.Dispose()
        }
        if ($bytes -lt 1) {
            throw "Downloading $Description from '$Uri' returned an empty file."
        }
        return (Get-Item -LiteralPath $destinationPath -Force -ErrorAction Stop).FullName
    }
    catch {
        Remove-Item -LiteralPath $destinationPath -Force -ErrorAction SilentlyContinue
        throw
    }
}

function Assert-AtlasFileSignature {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ExpectedSubjectCn,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $resolvedPath = Resolve-AtlasProtectedExecutionPath `
        -Path $Path -PathType Leaf -Description "The $Description installer"
    $signature = Get-AuthenticodeSignature -LiteralPath $resolvedPath -ErrorAction Stop
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or
        $null -eq $signature.SignerCertificate) {
        throw "The $Description installer's Authenticode signature is not valid (status: $($signature.Status)). Refusing to run an untrusted installer."
    }

    $subject = $signature.SignerCertificate.Subject
    $cnMatches = [regex]::Matches(
        $subject,
        '(?:^|,\s*)CN=',
        [Text.RegularExpressions.RegexOptions]::IgnoreCase -bor
            [Text.RegularExpressions.RegexOptions]::CultureInvariant
    )
    $escapedCn = [regex]::Escape($ExpectedSubjectCn)
    $subjectPattern = '(?:^|,\s*)CN=(?:"' + $escapedCn + '"|' + $escapedCn + ')(?=,|$)'
    if ($cnMatches.Count -ne 1 -or
        -not [regex]::IsMatch(
            $subject,
            $subjectPattern,
            [Text.RegularExpressions.RegexOptions]::IgnoreCase -bor
                [Text.RegularExpressions.RegexOptions]::CultureInvariant
        )) {
        throw "The $Description installer is signed by '$subject' instead of the expected publisher 'CN=$ExpectedSubjectCn'. Refusing to run."
    }
}

function Assert-AtlasFileHash {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-fA-F]{64}$')][string]$ExpectedSha256,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $resolvedPath = Resolve-AtlasProtectedExecutionPath `
        -Path $Path -PathType Leaf -Description "The $Description download"
    $actual = (Get-FileHash -LiteralPath $resolvedPath -Algorithm SHA256 -ErrorAction Stop).Hash
    if ($actual -ne $ExpectedSha256) {
        throw "The $Description download's SHA256 '$actual' does not match the expected '$ExpectedSha256'. Refusing to run an untrusted file."
    }
}

function Start-AtlasSoftwareInstaller {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [AllowEmptyCollection()][string[]]$ArgumentList = @(),
        [Parameter(Mandatory = $true)][string]$Description,
        [uint32[]]$SuccessExitCode = @(0),
        [ValidateRange(1, 86400)][int]$TimeoutSeconds = 1800
    )

    Write-Host "Installing $Description..."
    $workingDirectory = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($FilePath))
    $result = Invoke-AtlasContainedProcess `
        -FilePath $FilePath `
        -ArgumentList ([string[]]$ArgumentList) `
        -WorkingDirectory $workingDirectory `
        -Description "The $Description installer" `
        -TimeoutSeconds $TimeoutSeconds `
        -Hidden `
        -NoWindow
    $exitCode = [uint32]$result.ExitCodeUInt32
    if ($exitCode -notin $SuccessExitCode) {
        throw "Installing $Description failed with exit code $exitCode."
    }
}

function Start-AtlasSoftwareOptionalInstaller {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [AllowEmptyCollection()][string[]]$ArgumentList = @(),
        [Parameter(Mandatory = $true)][string]$Description,
        [uint32[]]$SuccessExitCode = @(0),
        [ValidateRange(1, 86400)][int]$TimeoutSeconds = 1800
    )

    try {
        Start-AtlasSoftwareInstaller `
            -FilePath $FilePath `
            -ArgumentList $ArgumentList `
            -Description $Description `
            -SuccessExitCode $SuccessExitCode `
            -TimeoutSeconds $TimeoutSeconds
        return $true
    }
    catch {
        if (Test-AtlasContainedProcessContainmentUnconfirmed -Exception $_.Exception) {
            throw
        }
        Write-AtlasLog -Level Warning -Message $_.Exception.Message
        return $false
    }
}

function Invoke-AtlasArchiveDownload {
    param(
        [Parameter(Mandatory = $true)][uri[]]$Uris,
        [Parameter(Mandatory = $true)][string]$Destination,
        [Parameter(Mandatory = $true)][string]$Sha256,
        [Parameter(Mandatory = $true)][long]$ExpectedBytes
    )

    # Each download verifies the same pinned bytes before returning. The pinned
    # downloader removes its own partial file on failure. Never retry installation.
    if (Test-Path -LiteralPath $Destination) { throw "Download destination already exists: $Destination" }
    # The limit covers the whole transfer, so scale it with the file: allow for
    # 32 KB/s from each source, between 1 and 15 minutes.
    $maximumSeconds = [int][Math]::Min(900, [Math]::Max(60, [Math]::Ceiling($ExpectedBytes / 32KB)))
    $failures = New-Object 'Collections.Generic.List[string]'
    foreach ($uri in $Uris) {
        try {
            Write-AtlasLog -Message "Downloading archive-app asset from $uri."
            return Invoke-AtlasPinnedDownload -Uri $uri -Destination $Destination `
                -Sha256 $Sha256 -ExpectedBytes $ExpectedBytes -MaximumSeconds $maximumSeconds
        }
        catch {
            $reason = $_.Exception.Message
            for ($exception = $_.Exception; $null -ne $exception; $exception = $exception.InnerException) {
                if ($exception -is [OperationCanceledException]) {
                    $reason = "the download didn't finish within its $maximumSeconds-second limit."
                    break
                }
            }
            $detail = "$uri : $reason"
            $failures.Add($detail)
            Write-AtlasLog -Level Warning -Message "Archive-app download failed: $detail"
        }
    }
    throw "All archive-app download sources failed: $($failures -join '; ')"
}

function Get-AtlasPinnedNanaZipReleaseAssets {
    [CmdletBinding()]
    param()

    # Pin upstream release digests in Atlas's files to avoid GitHub API requests
    # during installation. SourceForge is an official NanaZip fallback host;
    # its mirror selector can route downloads without a fixed regional mirror.
    $tag = '7.0.1843.0'
    foreach ($asset in @(
        @{ Name = "NanaZip_$tag.msixbundle"; Size = 11931446; Sha256 = 'f5b013afff37eca32ed7e318cb7d7d1b7a1e11f9765e4c2e9afafcde15eb085d' }
        @{ Name = "NanaZip_$tag.xml"; Size = 2667; Sha256 = '136a0dfcd5509f65434d83c770347c6638179efe16c8155bb1960aa071ccb45a' }
    )) {
        [pscustomobject]@{
            Tag = $tag
            Name = $asset.Name
            Size = $asset.Size
            Sha256 = $asset.Sha256
            Uri = [uri]"https://github.com/M2Team/NanaZip/releases/download/$tag/$($asset.Name)"
            FallbackUri = [uri]"https://downloads.sourceforge.net/project/nanazip/$tag/$($asset.Name)"
        }
    }
}

function Assert-AtlasNanaZipBundleIdentity {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]
        [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$')]
        [string]$Version
    )

    $resolvedPath = Resolve-AtlasProtectedExecutionPath `
        -Path $Path -PathType Leaf -Description 'The NanaZip MSIX bundle'
    Microsoft.PowerShell.Utility\Add-Type -AssemblyName System.IO.Compression -ErrorAction Stop

    $fileStream = $null
    $archive = $null
    $manifestStream = $null
    $xmlReader = $null
    try {
        $fileStream = [IO.File]::Open(
            $resolvedPath,
            [IO.FileMode]::Open,
            [IO.FileAccess]::Read,
            [IO.FileShare]::Read
        )
        $archive = New-Object IO.Compression.ZipArchive(
            $fileStream,
            [IO.Compression.ZipArchiveMode]::Read,
            $false
        )
        $entries = @($archive.Entries | Where-Object {
                $_.FullName -ceq 'AppxMetadata/AppxBundleManifest.xml'
            })
        if ($entries.Count -ne 1 -or
            $entries[0].Length -lt 1 -or $entries[0].Length -gt 1048576) {
            throw 'The NanaZip bundle does not contain one canonical AppxBundleManifest.xml of at most 1 MB.'
        }

        $settings = New-Object Xml.XmlReaderSettings
        $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
        $settings.XmlResolver = $null
        $settings.MaxCharactersInDocument = 1048576
        $settings.MaxCharactersFromEntities = 0
        $manifestStream = $entries[0].Open()
        $xmlReader = [Xml.XmlReader]::Create($manifestStream, $settings)
        $document = New-Object Xml.XmlDocument
        $document.XmlResolver = $null
        $document.Load($xmlReader)

        $root = $document.DocumentElement
        if ($null -eq $root -or $root.LocalName -cne 'Bundle' -or
            $root.NamespaceURI -cne 'http://schemas.microsoft.com/appx/2013/bundle') {
            throw 'The NanaZip bundle manifest root is not the canonical MSIX Bundle element.'
        }
        $identities = @($root.ChildNodes | Where-Object {
                $_.NodeType -eq [Xml.XmlNodeType]::Element -and
                $_.LocalName -ceq 'Identity'
            })
        if ($identities.Count -ne 1 -or
            $identities[0].GetAttribute('Name') -cne '40174MouriNaruto.NanaZip' -or
            $identities[0].GetAttribute('Publisher') -cne 'CN=E310A153-74A9-4D81-800B-857A8D58408A' -or
            $identities[0].GetAttribute('Version') -cne $Version) {
            throw 'The NanaZip bundle identity does not match the reviewed Store package name, publisher, and release version.'
        }
    }
    finally {
        if ($null -ne $xmlReader) { $xmlReader.Dispose() }
        if ($null -ne $manifestStream) { $manifestStream.Dispose() }
        if ($null -ne $archive) { $archive.Dispose() }
        if ($null -ne $fileStream) { $fileStream.Dispose() }
    }
}

function Get-AtlasDismProvisioningCommands {
    [CmdletBinding()]
    param()

    $manifestPath = Join-Path -Path ([Environment]::GetFolderPath('System')) `
        -ChildPath 'WindowsPowerShell\v1.0\Modules\Dism\Dism.psd1'
    $resolvedManifest = Resolve-AtlasProtectedExecutionPath `
        -Path $manifestPath -PathType Leaf -Description 'The inbox DISM module manifest'
    $modules = @(Import-Module -Name $resolvedManifest -Force -PassThru -ErrorAction Stop)
    $expectedGuid = [guid]'389c464d-8b8d-48e9-aafe-6d8a590d6798'
    $expectedBase = [IO.Path]::GetFullPath((Split-Path -Parent $resolvedManifest)).TrimEnd('\')
    if ($modules.Count -ne 1 -or $modules[0].Name -cne 'Dism' -or
        $modules[0].Guid -ne $expectedGuid -or
        -not [IO.Path]::GetFullPath($modules[0].ModuleBase).TrimEnd('\').Equals(
            $expectedBase,
            [StringComparison]::OrdinalIgnoreCase
        )) {
        throw "The inbox DISM module did not load from its exact manifest at '$resolvedManifest'."
    }

    $getPackage = $modules[0].ExportedCommands['Get-AppxProvisionedPackage']
    $addPackage = $modules[0].ExportedCommands['Add-AppxProvisionedPackage']
    if ($null -eq $getPackage -or $null -eq $addPackage -or
        $getPackage.Module.Guid -ne $expectedGuid -or
        $addPackage.Module.Guid -ne $expectedGuid) {
        throw 'The exact inbox DISM module did not export its expected AppX provisioning commands.'
    }

    return [pscustomobject]@{
        GetProvisionedPackage = $getPackage
        AddProvisionedPackage = $addPackage
    }
}

function Test-AtlasNanaZipProvisioned {
    [CmdletBinding()]
    param(
        [AllowEmptyCollection()][object[]]$Package = @(),
        [string]$Version
    )

    foreach ($candidate in $Package) {
        if ($null -eq $candidate.PSObject.Properties['DisplayName'] -or
            [string]$candidate.DisplayName -cne '40174MouriNaruto.NanaZip') {
            continue
        }
        if ([string]::IsNullOrEmpty($Version)) {
            return $true
        }
        if ($null -ne $candidate.PSObject.Properties['Version'] -and
            [string]$candidate.Version -ceq $Version) {
            return $true
        }
    }
    return $false
}

function Test-AtlasVisualCppRuntimeCurrent {
    param(
        [Parameter(Mandatory = $true)][ValidateSet('x86', 'x64')][string]$Architecture,
        [Parameter(Mandatory = $true)][string]$InstallerPath
    )

    # Compare against the signed package we actually downloaded, not a fixed
    # minimum that would prevent future runtime updates.
    $requiredVersion = $null
    try {
        $packageVersion = [string](Get-Item -LiteralPath $InstallerPath -ErrorAction Stop).VersionInfo.ProductVersion
        if (-not [version]::TryParse($packageVersion.TrimStart('v'), [ref]$requiredVersion) -or
            $requiredVersion.Major -ne 14) {
            return $false
        }
    }
    catch { return $false }

    foreach ($prefix in @('SOFTWARE\Microsoft', 'SOFTWARE\Wow6432Node\Microsoft')) {
        $path = "Registry::HKEY_LOCAL_MACHINE\$prefix\VisualStudio\14.0\VC\Runtimes\$Architecture"
        try {
            $runtime = Get-ItemProperty -LiteralPath $path -ErrorAction Stop
            $installedVersion = $null
            if ($runtime.Installed -eq 1 -and
                [version]::TryParse(([string]$runtime.Version).TrimStart('v'), [ref]$installedVersion) -and
                $installedVersion.Major -eq 14 -and $installedVersion -ge $requiredVersion) {
                Write-AtlasLog -Message "Visual C++ Runtime 2015+-$Architecture already installed ($installedVersion; package $requiredVersion); skipping installation."
                return $true
            }
        }
        catch {
            # Missing, unreadable, or incomplete registration is not proof that
            # the prerequisite is met. Let the installer handle those cases.
            continue
        }
    }
    return $false
}

function Install-AtlasVisualCppRuntimes {
    param([Parameter(Mandatory = $true)][string]$TempDir)

    # Keep native setup logs outside disposable software staging. Each attempt gets
    # its own directory, so retries cannot erase the error we need to investigate.
    $logRoot = Join-Path ([Environment]::GetFolderPath('Windows')) ('AtlasOS\Install\Diagnostics\vcredist-' + [guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($logRoot)
    Write-AtlasLog -Message "Visual C++ installer logs: $logRoot"
    $legacyArgs = @('/q', '/norestart')
    $modernArgs = @('/install', '/quiet', '/norestart')
    $vcredists = @(
        [pscustomobject]@{ Uri = 'https://download.microsoft.com/download/8/B/4/8B42259F-5D70-43F4-AC2E-4B208FD8D66A/vcredist_x64.exe'; Name = '2005-x64'; Arguments = @('/c', '/q'); Extract = $true }
        [pscustomobject]@{ Uri = 'https://download.microsoft.com/download/8/B/4/8B42259F-5D70-43F4-AC2E-4B208FD8D66A/vcredist_x86.exe'; Name = '2005-x86'; Arguments = @('/c', '/q'); Extract = $true }
        [pscustomobject]@{ Uri = 'https://download.microsoft.com/download/5/D/8/5D8C65CB-C849-4025-8E95-C3966CAFD8AE/vcredist_x64.exe'; Name = '2008-x64'; Arguments = $legacyArgs; Extract = $false }
        [pscustomobject]@{ Uri = 'https://download.microsoft.com/download/5/D/8/5D8C65CB-C849-4025-8E95-C3966CAFD8AE/vcredist_x86.exe'; Name = '2008-x86'; Arguments = $legacyArgs; Extract = $false }
        [pscustomobject]@{ Uri = 'https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x64.exe'; Name = '2010-x64'; Arguments = $legacyArgs; Extract = $false }
        [pscustomobject]@{ Uri = 'https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x86.exe'; Name = '2010-x86'; Arguments = $legacyArgs; Extract = $false }
        [pscustomobject]@{ Uri = 'https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x64.exe'; Name = '2012-x64'; Arguments = $modernArgs; Extract = $false }
        [pscustomobject]@{ Uri = 'https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x86.exe'; Name = '2012-x86'; Arguments = $modernArgs; Extract = $false }
        [pscustomobject]@{ Uri = 'https://download.visualstudio.microsoft.com/download/pr/10912041/cee5d6bca2ddbcd039da727bf4acb48a/vcredist_x64.exe'; Name = '2013-x64'; Arguments = $modernArgs; Extract = $false }
        [pscustomobject]@{ Uri = 'https://download.visualstudio.microsoft.com/download/pr/10912113/5da66ddebb0ad32ebd4b922fd82e8e25/vcredist_x86.exe'; Name = '2013-x86'; Arguments = $modernArgs; Extract = $false }
        [pscustomobject]@{ Uri = 'https://aka.ms/vs/17/release/vc_redist.x64.exe'; Name = '2015+-x64'; Arguments = $modernArgs; Extract = $false }
        [pscustomobject]@{ Uri = 'https://aka.ms/vs/17/release/vc_redist.x86.exe'; Name = '2015+-x86'; Arguments = $modernArgs; Extract = $false }
    )
    $msiExecPath = Join-Path -Path ([Environment]::GetFolderPath('System')) -ChildPath 'msiexec.exe'
    $failedInstallers = New-Object Collections.Generic.List[string]

    foreach ($entry in $vcredists) {
        $vcName = $entry.Name
        Write-AtlasLog -Message "Preparing Visual C++ Runtime $vcName from $($entry.Uri)."
        $vcExePath = Join-Path -Path $TempDir -ChildPath "vcredist-$vcName.exe"
        Invoke-AtlasSoftwareDownload -Uri $entry.Uri -Destination $vcExePath -Description "Visual C++ Runtime $vcName"
        Assert-AtlasFileSignature -Path $vcExePath -ExpectedSubjectCn 'Microsoft Corporation' -Description "Visual C++ Runtime $vcName"

        if ($vcName -match '^2015\+-(x64|x86)$' -and
            (Test-AtlasVisualCppRuntimeCurrent -Architecture $Matches[1] -InstallerPath $vcExePath)) {
            continue
        }

        if ($entry.Extract) {
            $msiDir = Join-Path -Path $TempDir -ChildPath "vcredist-$vcName"
            $extractArguments = @($entry.Arguments) + @('/t:' + $msiDir)
            if (-not (Start-AtlasSoftwareOptionalInstaller `
                    -FilePath $vcExePath `
                    -ArgumentList $extractArguments `
                    -Description "Visual C++ Runtime $vcName extractor" `
                    -SuccessExitCode @(0, 3010))) {
                $failedInstallers.Add("$vcName extractor")
                continue
            }
            $null = Resolve-AtlasProtectedExecutionPath `
                -Path $msiDir -PathType Container -Description "The $vcName MSI directory"
            $msiPaths = @(Get-ChildItem -LiteralPath $msiDir -File -Filter '*.msi' -ErrorAction Stop)
            if ($msiPaths.Count -lt 1) {
                Write-AtlasLog -Level Warning -Message "The $vcName extractor produced no MSI package."
                $failedInstallers.Add("$vcName extracted MSI")
                continue
            }
            foreach ($msi in $msiPaths) {
                $resolvedMsiPath = Resolve-AtlasProtectedExecutionPath `
                    -Path $msi.FullName `
                    -PathType Leaf `
                    -Description "The Visual C++ Runtime $vcName MSI"
                $msiArguments = @(
                    '/log'
                    (Join-Path -Path $logRoot -ChildPath "$vcName-$($msi.BaseName).log")
                    '/i'
                    $resolvedMsiPath
                    '/qn'
                    '/norestart'
                    'ALLUSERS=1'
                    'REBOOT=ReallySuppress'
                )
                if (-not (Start-AtlasSoftwareOptionalInstaller `
                        -FilePath $msiExecPath `
                        -ArgumentList $msiArguments `
                        -Description "Visual C++ Runtime $vcName MSI" `
                        -SuccessExitCode @(0, 3010))) {
                    $failedInstallers.Add("$vcName MSI")
                }
            }
        }
        else {
            $installerArguments = @($entry.Arguments)
            if ($vcName -match '^(2012|2013|2015\+)') {
                $installerArguments += @('/log', (Join-Path $logRoot "$vcName.log"))
            }
            if (-not (Start-AtlasSoftwareOptionalInstaller `
                    -FilePath $vcExePath `
                    -ArgumentList ([string[]]$installerArguments) `
                    -Description "Visual C++ Runtime $vcName" `
                    -SuccessExitCode @(0, 3010))) {
                $failedInstallers.Add($vcName)
            }
        }
    }

    if ($failedInstallers.Count -gt 0) {
        throw "One or more Visual C++ runtime installers failed: $($failedInstallers -join ', ')."
    }
}

function Install-AtlasNanaZip {
    param(
        [Parameter(Mandatory = $true)][string]$TempDir,
        [Parameter(Mandatory = $true)][object[]]$Assets,
        [Parameter(Mandatory = $true)]$DismCommands
    )

    $provisioningStarted = $false
    try {
        if ($Assets.Count -ne 2 -or
            $null -eq $DismCommands.GetProvisionedPackage -or
            $null -eq $DismCommands.AddProvisionedPackage) {
            throw 'NanaZip installation requires exactly two verified assets and exact DISM commands.'
        }

        $tag = [string]$Assets[0].Tag
        if ($tag -cnotmatch '^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$') {
            throw 'NanaZip asset metadata does not contain a stable release tag.'
        }
        $bundleName = "NanaZip_$tag.msixbundle"
        $licenseName = "NanaZip_$tag.xml"
        $expectedNames = @($bundleName, $licenseName)
        if (@($Assets | Where-Object {
                    $null -eq $_.PSObject.Properties['Name'] -or
                    $null -eq $_.PSObject.Properties['Uri'] -or
                    $null -eq $_.PSObject.Properties['Sha256'] -or
                    $null -eq $_.PSObject.Properties['Size'] -or
                    $null -eq $_.PSObject.Properties['Tag'] -or
                    [string]$_.Tag -cne $tag -or
                    [string]$_.Name -cnotin $expectedNames
                }).Count -ne 0 -or
            @($Assets | Select-Object -ExpandProperty Name -Unique).Count -ne 2) {
            throw 'NanaZip assets do not describe one exact release bundle and license pair.'
        }

        $nanaZipDirectory = Join-Path -Path $TempDir -ChildPath 'nanazip'
        if (Test-Path -LiteralPath $nanaZipDirectory) {
            throw "The NanaZip staging path '$nanaZipDirectory' already exists."
        }
        $nanaZipPath = New-Item -Path $nanaZipDirectory -ItemType Directory -ErrorAction Stop
        $null = Resolve-AtlasProtectedExecutionPath `
            -Path $nanaZipPath.FullName -PathType Container -Description 'The NanaZip staging directory'

        $assetPaths = @{}
        foreach ($asset in $Assets) {
            $destination = Join-Path -Path $nanaZipPath.FullName -ChildPath $asset.Name
            $uris = @($asset.Uri)
            if ($null -ne $asset.PSObject.Properties['FallbackUri']) { $uris += $asset.FallbackUri }
            $assetPaths[$asset.Name] = Invoke-AtlasArchiveDownload `
                -Uris $uris `
                -Destination $destination `
                -Sha256 $asset.Sha256 `
                -ExpectedBytes $asset.Size
        }
        if (-not $assetPaths.ContainsKey($bundleName) -or
            -not $assetPaths.ContainsKey($licenseName)) {
            throw 'NanaZip release metadata did not resolve the exact bundle and license pair.'
        }
        Assert-AtlasNanaZipBundleIdentity -Path $assetPaths[$bundleName] -Version $tag

        $appxArgs = @{
            Online      = $true
            PackagePath = $assetPaths[$bundleName]
            LicensePath = $assetPaths[$licenseName]
            ErrorAction = 'Stop'
        }
        # DISM may change machine package state from here on, so a failure past
        # this point rethrows the original error.
        $provisioningStarted = $true
        & $DismCommands.AddProvisionedPackage @appxArgs | Out-Null
        $installed = @(& $DismCommands.GetProvisionedPackage -Online -ErrorAction Stop)
        if (-not (Test-AtlasNanaZipProvisioned -Package $installed -Version $tag)) {
            throw "DISM returned without provisioning NanaZip version '$tag'."
        }
        Write-Host 'Installed NanaZip!'
        return $true
    }
    catch {
        if ($provisioningStarted -or
            (Test-AtlasContainedProcessContainmentUnconfirmed -Exception $_.Exception)) {
            throw
        }
        throw "NanaZip could not be installed from the verified download sources. $($_.Exception.Message)"
    }
}

function Install-AtlasArchiveTool {
    param([Parameter(Mandatory = $true)][string]$TempDir)

    # The install engine runs without a desktop, so nobody could answer a question
    # about replacing 7-Zip. Keep the archive tool the user already chose.
    if (Test-Path -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\7-Zip') {
        Write-AtlasLog -Message '7-Zip is installed, so Atlas keeps it and skips NanaZip.'
        return
    }

    try {
        $dismCommands = Get-AtlasDismProvisioningCommands
        $provisionedPackages = @(& $dismCommands.GetProvisionedPackage -Online -ErrorAction Stop)
    }
    catch {
        throw "NanaZip provisioning is unavailable. $($_.Exception.Message)"
    }

    if (Test-AtlasNanaZipProvisioned -Package $provisionedPackages) {
        Write-Host 'NanaZip is already installed, skipping installation.'
        return
    }

    if (-not (Install-AtlasNanaZipFromStore -DismCommands $dismCommands)) {
        try {
            $assets = @(Get-AtlasPinnedNanaZipReleaseAssets)
        }
        catch {
            throw "NanaZip release integrity could not be established. $($_.Exception.Message)"
        }
        $null = Install-AtlasNanaZip `
            -TempDir $TempDir `
            -Assets $assets `
            -DismCommands $dismCommands
    }
}

function Install-AtlasDirectXRuntime {
    param([Parameter(Mandatory = $true)][string]$TempDir)

    $installerPath = Join-Path -Path $TempDir -ChildPath 'directx.exe'
    $extractPath = Join-Path -Path $TempDir -ChildPath 'directx'
    Invoke-AtlasSoftwareDownload -Uri 'https://download.microsoft.com/download/8/4/A/84A35BF1-DAFE-4AE8-82AF-AD2AE20B6B14/directx_Jun2010_redist.exe' -Destination $installerPath -Description 'legacy DirectX runtimes'
    # Fixed June 2010 URL: the file can never change, so pin its hash in addition to the signature.
    Assert-AtlasFileHash -Path $installerPath -ExpectedSha256 '053f76dcbb28802e23341b6a787e3b0791c0fa5c8d4d011b1044172dbf89c73b' -Description 'legacy DirectX runtimes'
    Assert-AtlasFileSignature -Path $installerPath -ExpectedSubjectCn 'Microsoft Corporation' -Description 'legacy DirectX runtimes'
    Start-AtlasSoftwareInstaller `
        -FilePath $installerPath `
        -ArgumentList @('/q', '/c', ('/t:' + $extractPath)) `
        -Description 'legacy DirectX runtime extractor'
    Start-AtlasSoftwareInstaller `
        -FilePath (Join-Path -Path $extractPath -ChildPath 'dxsetup.exe') `
        -ArgumentList @('/silent') `
        -Description 'legacy DirectX runtimes'
}

function Install-AtlasSoftware {
    <#
    .SYNOPSIS
        Downloads and installs the given software components at install time. Each
        component failure is logged as a warning and the remaining components still
        install.
    .OUTPUTS
        $true when every component installed successfully and protected staging was
        removed, $false otherwise.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('SevenZip', 'VCRedist', 'DirectX')]
        [string[]]$Component
    )

    Assert-AtlasPrivilege -Administrator

    $componentMap = Get-AtlasSoftwareComponentMap
    $tempDir = New-AtlasProtectedStagingDirectory
    $allSucceeded = $true
    $cleanupStaging = $true
    try {
        foreach ($name in $Component) {
            try {
                Write-AtlasLog -Message "Installing software component '$name'."
                $null = & $componentMap[$name] -TempDir $tempDir
            }
            catch {
                if (Test-AtlasContainedProcessContainmentUnconfirmed -Exception $_.Exception) {
                    $cleanupStaging = $false
                }
                Write-AtlasLog -Level Warning -Message "Installing software component '$name' failed: $($_.Exception.Message)"
                $allSucceeded = $false
            }
        }
    }
    catch {
        if (Test-AtlasContainedProcessContainmentUnconfirmed -Exception $_.Exception) {
            $cleanupStaging = $false
        }
        Write-AtlasLog -Level Warning -Message "Software installer setup failed: $($_.Exception.Message)"
        $allSucceeded = $false
    }
    finally {
        if (-not $cleanupStaging) {
            Write-AtlasLog -Level Warning -Message "Protected software staging was retained at '$tempDir' because process-tree containment was not confirmed."
            $allSucceeded = $false
        }
        else {
            try {
                if (Test-Path -LiteralPath $tempDir) {
                    $resolvedStaging = Resolve-AtlasProtectedExecutionPath `
                        -Path $tempDir `
                        -PathType Container `
                        -Description 'The software staging directory'
                    Remove-Item -LiteralPath $resolvedStaging -Force -Recurse -ErrorAction Stop
                }
            }
            catch {
                Write-AtlasLog -Level Warning -Message "Protected software staging cleanup failed: $($_.Exception.Message)"
                $allSucceeded = $false
            }
        }
    }

    return $allSucceeded
}
