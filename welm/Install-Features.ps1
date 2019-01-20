Set-StrictMode -Version 2

Function Test-InternetConnection() {
    [CmdletBinding()]
    [OutputType([bool])]
    Param(
        [Parameter(Mandatory=$false, HelpMessage='The URL to test connectivity to')]
        [ValidateNotNullOrEmpty()]
        [string]$Url = 'http://www.msftncsi.com/ncsi.txt',

        [Parameter(Mandatory=$false, HelpMessage='The timeout period in seconds')]
        [int]$Timeout = 5
    )

    $connected = $false
    $Url = $Url.ToLower()

    if (-not($Url.StartsWith('http://') -or $Url.StartsWith('https://'))) {
        $Url = 'http://{0}' -f $Url
    }

    try {
        $proxyUri = [System.Net.WebRequest]::GetSystemWebProxy().GetProxy($Url)

        if ($Url -eq $proxyUri.OriginalString) {
            $response = Invoke-WebRequest -Uri $Url -TimeoutSec $Timeout -Verbose:$false
        } else {
            $response = Invoke-WebRequest -Uri $Url -TimeoutSec $Timeout -Verbose:$false -Proxy $proxyUri -ProxyUseDefaultCredentials
        }

        $connected = $response.StatusCode -eq 200
    } catch {}

    return $connected
}

Function Invoke-Process() {
    <#
    .SYNOPSIS
    Executes a process.

    .DESCRIPTION
    Executes a process and waits for it to exit.

    .PARAMETER Path
    The path of the file to execute.

    .PARAMETER Arguments
    THe arguments to pass to the executable. Arguments with spaces in them are automatically quoted.

    .PARAMETER PassThru
    Return the results as an object.

    .EXAMPLE
    Invoke-Process -Path 'C:\Windows\System32\whoami.exe'

    .EXAMPLE
    Invoke-Process -Path 'C:\Windows\System32\whoami.exe' -Arguments '/groups'

    .EXAMPLE
    Invoke-Process -Path 'C:\Windows\System32\whoami.exe' -Arguments '/groups' -PassThru

    .EXAMPLE
    Invoke-Process -Path 'lgpo.exe' -Arguments '/g','C:\path to gpo folder' -PassThru
    #>
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true, HelpMessage='The path of the file to execute')]
        [ValidateNotNullOrEmpty()]
        #[ValidateScript({Test-Path -Path $_ -PathType Leaf})]
        #[ValidateScript({[System.IO.File]::Exists($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($_))})]
        [string]$Path,

        [Parameter(Mandatory=$false, HelpMessage='The arguments to pass to the executable')]
        [ValidateNotNullOrEmpty()]
        [string[]]$Arguments,

        [Parameter(Mandatory=$false, HelpMessage='Return the results as an object')]
        [switch]$PassThru
    )

    $parameters = $PSBoundParameters

    $escapedArguments = ''

    if ($parameters.ContainsKey('Arguments')) {
        $Arguments | ForEach-Object {
            if ($_.Contains(' ')) {
                $escapedArguments = $escapedArguments,("`"{0}`"" -f $_) -join ' '
            } else {
                $escapedArguments = $escapedArguments,$_ -join ' '
            }
        }
    }

    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = $Path
    $processInfo.RedirectStandardError = $true
    $processInfo.RedirectStandardOutput = $true
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true
    $processInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    $processInfo.Arguments = $escapedArguments.Trim()
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $processInfo
    $process.Start() | Out-Null
    $output = $process.StandardOutput.ReadToEnd()
    $process.WaitForExit()

    $exitCode = $process.ExitCode

    if($PassThru) {
        return [pscustomobject]@{
            'ExitCode' = $exitCode;
            'Output' = $output;
            'Process' = $process;
        }
    }
}

Function Get-OperatingSystemVersion() {
    <#
    .SYNOPSIS
    Gets the operating system version.

    .DESCRIPTION
    Gets the operating system version.

    .PREREQUISITES
    Windows 7 x86/x64 and later.

    .EXAMPLE
    Get-OperatingSystemVersion
    #>
    [CmdletBinding()]
    [OutputType([System.Version])]
    Param()

    $major = 0
    $minor = 0
    $build = 0
    $revision = 0

    $currentVersionPath = 'HKLM:\Software\Microsoft\Windows NT\CurrentVersion'

    $isWindows10orLater = $null -ne (Get-ItemProperty -Path $currentVersionPath -ErrorAction SilentlyContinue | Select-Object -ExpandProperty 'CurrentMajorVersionNumber' -ErrorAction SilentlyContinue)

    #todo: backport to Get-Item so it works on PowerShell 2.0

    if($isWindows10orLater) {
        $major = [Uint32](Get-ItemProperty -Path $currentVersionPath -ErrorAction SilentlyContinue | Select-Object -ExpandProperty 'CurrentMajorVersionNumber' -ErrorAction SilentlyContinue)
        $minor = [UInt32](Get-ItemProperty -Path $currentVersionPath -ErrorAction SilentlyContinue | Select-Object -ExpandProperty 'CurrentMinorVersionNumber' -ErrorAction SilentlyContinue)
        $build = [UInt32](Get-ItemProperty -Path $currentVersionPath -ErrorAction SilentlyContinue | Select-Object -ExpandProperty 'CurrentBuildNumber' -ErrorAction SilentlyContinue)
        $revision = [UInt32](Get-ItemProperty -Path $currentVersionPath -ErrorAction SilentlyContinue | Select-Object -ExpandProperty 'UBR' -ErrorAction SilentlyContinue)
    } else {
        $major = [Uint32]((Get-ItemProperty -Path $currentVersionPath -ErrorAction SilentlyContinue | Select-Object -ExpandProperty 'CurrentVersion' -ErrorAction SilentlyContinue) -split '\.')[0]
        $minor = [UInt32]((Get-ItemProperty -Path $currentVersionPath -ErrorAction SilentlyContinue | Select-Object -ExpandProperty 'CurrentVersion' -ErrorAction SilentlyContinue) -split '\.')[1]
        $build = [UInt32](Get-ItemProperty -Path $currentVersionPath -ErrorAction SilentlyContinue | Select-Object -ExpandProperty 'CurrentBuild' -ErrorAction SilentlyContinue)
        #revision could be service pack level on downlevel OSes
    }

    return [System.Version]('{0}.{1}.{2}.{3}' -f $major,$minor,$build,$revision)
}

Function Get-WindowsMediaPath() {
    <#
    .SYNOPSIS
    Gets the Windows install media path.

    .DESCRIPTION
    Gets the Windows install media path.

    .EXAMPLE
    Get-WindowsMediaPath
    #>
    [CmdletBinding()]
    [OutputType([bool])]
    Param()

    $mediaPath = ''

    # get CD/DVD drives
    $drives = @(Get-WmiObject -Class Win32_LogicalDisk -Filter 'DriveType=5')

    if ($drives.Count -ne 0) {
        foreach($drive in $drives) {
            $driveLetter = ($drive | Select-Object DeviceID -ExpandProperty DeviceID)

            $size = ($drive | Select-Object Size -ExpandProperty Size)

            if($null -ne $size) {
                continue
            }

            if($size -eq 0) {
                continue
            }

            if((Join-Path $driveLetter '\sources\sxs' | Test-Path -PathType Container)) {
                $mediaPath = Join-Path $driveLetter '\sources\sxs'
                break
            } else {
                continue
            }
        }
    }

    return $mediaPath
}

Function Invoke-ParseFeatures() {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true, HelpMessage='The path of the file to execute')]
        [ValidateNotNullOrEmpty()]
        [string]$Text
    )

    $features = New-Object System.Collections.Generic.List[psobject]

    $featureLine = $Text.Split([Environment]::NewLine,[System.StringSplitOptions]::RemoveEmptyEntries)

    for ($i=7; $i -lt $featureLine.Count-1; $i++) {
        $indexOfBar = $featureLine[$i].IndexOf('|')

        $featureName = $featureLine[$i].Substring(0,$indexOfBar).Trim()

        $featureState = $featureLine[$i].Substring($indexOfBar+2, $featureLine[$i].Length-$indexOfBar-2).Trim()

        $feature = [pscustomobject]@{
            Name = $featureName;
            State = $featureState
        }

        $features.Add($feature)
    }

    return ,$features
}

Function Invoke-InstallFeatures() {
    [CmdletBinding()]
    Param()

    $functionStart = [System.DateTime]::Now

    $log = New-Object System.Text.StringBuilder

    $dismPath = ''

    #if ([IntPtr]::Size -eq 8) {
    #    $dismPath  = '{0}\System32\dism.exe' -f $env:SystemRoot
    #} else {
    #    $dismPath  = '{0}\SysWOW64\dism.exe' -f $env:SystemRoot
    #}

    $dismPath = 'dism.exe'

    $timestamp = '{0:yyyMMddHHmmss}' -f [System.DateTime]::Now

    $osVersion = Get-OperatingSystemVersion
    $version = [decimal]('{0}.{1}' -f $osVersion.Major,$osVersion.Minor)

    if ($version -lt 6.1) {
        $message = 'dism does not exist on this OS version'
        [void]$log.AppendLine($message)

        $logData = $log.ToString()

        $functionEnd = [System.DateTime]::Now

        $functionTimespan = [System.TimeSpan]($functionEnd - $functionStart)

        $returnValue = [pscustomobject]@{
            NeedsReboot = $false;
            NeedsInstall = $false; # no way to automate install on this OS
            Log = $logData;
            Elapsed = $functionTimespan;
        }

        return $returnValue
    }

    $connected = Test-InternetConnection

    $mediaPath = ''

    if($version -ge 6.2 -and -not($connected)) {
        $mediaPath = Get-WindowsMediaPath

        if ($mediaPath -eq '') {
            $message ='No CD/DVD drive, no CD/DVD inserted, or the CD/DVD inserted is not Windows installation media'
            [void]$log.AppendLine($message)

            $logData = $log.ToString()

            $functionEnd = [System.DateTime]::Now

            $functionTimespan = [System.TimeSpan]($functionEnd - $functionStart)

            $returnValue = [pscustomobject]@{
                NeedsReboot = $false;
                NeedsInstall = $true;
                Log = $logData;
                Elapsed = $functionTimespan;
            }

            return $returnValue
        }
    }

    $osType = Get-WmiObject Win32_OperatingSystem | Select-Object -Property ProductType -ExpandProperty ProductType

    $result = Invoke-Process -Path $dismPath -Arguments '/online','/get-features','/format:table' -PassThru

    $featuresText = $result.Output

    # todo: check for features that can't be installed
    if (-not($featuresText.Contains('Disabled'))) {
        $message = 'All features are installed'
        Write-Verbose -Message $message
        [void]$log.AppendLine($message)

        $logData = $log.ToString()

        $functionEnd = [System.DateTime]::Now

        $functionTimespan = [System.TimeSpan]($functionEnd - $functionStart)

        $returnValue = [pscustomobject]@{
            NeedsReboot = $false;
            NeedsInstall = $false;
            Log = $logData;
            Elapsed = $functionTimespan;
        }

        return $returnValue
    }

    $matches = $featuresText | Select-String -Pattern 'Disabled' -AllMatches
    $featuresToInstall = $matches.Matches.Count
    $featuresSuccessfullyInstalled = 0

    [void]$log.AppendLine('DISM feature status before')
    [void]$log.AppendLine($featuresText)

    $errorCode = $result.ExitCode

    if($errorCode -ne 0) {
        if ($errorCode -eq 183) {
            $message = 'dism is busy so wait or reboot if this keeps happening'
        } else {
            $message = "dism returned an error code of $errorCode"
        }

        [void]$log.AppendLine($message)

        $logData = $log.ToString()

        $functionEnd = [System.DateTime]::Now

        $functionTimespan = [System.TimeSpan]($functionEnd - $functionStart)

        $returnValue = [pscustomobject]@{
            NeedsReboot = $true;
            NeedsInstall = $true;
            Log = $logData;
            Elapsed = $functionTimespan;
        }

        return $returnValue
    }

    $needsReboot = $false

    $features = Invoke-ParseFeatures -Text $featuresText

    $features | ForEach-Object {
        $message = ''

        $featureName = $_.Name

        $featureState = $_.State

        if ($featureState -eq "Enabled") {
            $message = "$featureName was skipped because it was already installed"

            Write-Verbose -Message $message
            [void]$log.AppendLine($message)
        } else {
            $arguments = New-Object System.Collections.Generic.List[string]
            $arguments.AddRange([string[]]@('/online','/enable-feature',"/featureName:$featureName",'/NoRestart','/Quiet'))

            if ($version -ge 6.2) {
                $arguments.Add('/All')
            }

            if (($featureName -eq 'NetFx3') -and -not($connected) -and ($featureState -eq 'Disabled With Payload Removed')) {
                $arguments.Add('/LimitAccess')
                $arguments.Add("/Source:$mediaPath")
            }

            $featureStart = [System.DateTime]::Now

            $result = Invoke-Process -Path $dismPath -Arguments ([string[]]$arguments.ToArray()) -PassThru

            $errorCode = $result.ExitCode
            $message = $result.Output

            $featureEnd = [System.DateTime]::Now

            $timespan = [System.TimeSpan]($featureEnd - $featureStart)

            if ($errorCode -eq 0) {
                $featuresSuccessfullyInstalled++
                $message = ('{0} install succeeded. {1} of {2}. {3} minutes {4} seconds' -f $featureName,$featuresSuccessfullyInstalled,$featuresToInstall,$timespan.Minutes,$timespan.Seconds)

                Write-Verbose -Message $message
                [void]$log.AppendLine($message)
            } elseif ($errorCode -eq 3010) {
                $featuresSuccessfullyInstalled++
                $needsReboot = $true
                $message = ('{0} install succeeded and requires a system restart. {1} of {2}. {3} minutes {4} seconds' -f $featureName,$featuresSuccessfullyInstalled,$featuresToInstall,$timespan.Minutes,$timespan.Seconds)

                Write-Verbose -Message $message
                [void]$log.AppendLine($message)
            } elseif ($errorCode -eq 50) {
                $needsInstall = $true
                $message = "$featureName install failed. a required feature was not already installed"

                Write-Warning -Message $message
                [void]$log.AppendLine($message)
            } else {
                $needsInstall = $true
                $message = "$featureName install failed with exit code $errorCode and message $message"

                Write-Warning -Message $message
                [void]$log.AppendLine($message)
            }
        }
    }

    $result = Invoke-Process -Path $dismPath -Arguments '/online','/get-features','/format:table' -PassThru

    $featuresText = $result.Output

    [void]$log.AppendLine('DISM feature status after')
    [void]$log.AppendLine($featuresText)

    $needsInstall = $false

    # todo: check for any features that are not installed yet (well, any except for known bads (e.g. breaks keyboard integration)) rather than check for anything being in the Disabled state
    # todo: check for features that can't be installed
    if ($featuresText.Contains('Disabled')) {
        $needsInstall = $true
    }

    $logData = $log.ToString()

    $functionEnd = [System.DateTime]::Now

    $functionTimespan = [System.TimeSpan]($functionEnd - $functionStart)

    $returnValue = [pscustomobject]@{
        NeedsReboot = $needsReboot;
        NeedsInstall = $needsInstall;
        Log = $logData;
        Elapsed = $functionTimespan;
    }

    return $returnValue
}