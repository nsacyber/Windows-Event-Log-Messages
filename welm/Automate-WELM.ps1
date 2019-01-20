Param(
    [Parameter(Mandatory=$true, HelpMessage='VM NAT network address')]
    [ValidateNotNullOrEmpty()]
    [string]$VMNetwork,

    [Parameter(Mandatory=$true, HelpMessage='VM NAT network subnet length aka prefix size')]
    [ValidateNotNullOrEmpty()]
    [int]$VMNetworkPrefixSize,

    [Parameter(Mandatory=$true, HelpMessage='The name of a VM, that exists at the $HostVMPath, that will be cloned')]
    [ValidateNotNullOrEmpty()]
    [string]$VMName,

    [Parameter(Mandatory=$true, HelpMessage='The path on the host where the payload for the VM is stored')]
    [ValidateNotNullOrEmpty()]
    [string]$HostStagingPath,

    [Parameter(Mandatory=$true, HelpMessage='The path on the host where the VM payload results are stored')]
    [ValidateNotNullOrEmpty()]
    [string]$HostResultPath,

    [Parameter(Mandatory=$true, HelpMessage='The path in the VM where the payload is stored')]
    [ValidateNotNullOrEmpty()]
    [string]$VMStagingPath,

    [Parameter(Mandatory=$true, HelpMessage='The path in the VM where the payload results are stored')]
    [ValidateNotNullOrEmpty()]
    [string]$VMResultPath,

    [Parameter(Mandatory=$true, HelpMessage='The path on the host where the PowerShell transcript is stored')]
    [ValidateNotNullOrEmpty()]
    [string]$HostTranscriptPath,

    [Parameter(Mandatory=$false, HelpMessage='Do not delete the cloned virtual machine (useful for debugging)')]
    [ValidateNotNullOrEmpty()]
    [switch]$NoDelete,

    [Parameter(Mandatory=$false, HelpMessage='Disable Windows Update')]
    [ValidateNotNullOrEmpty()]
    [switch]$NoWindowsUpdate,

    [Parameter(Mandatory=$false, HelpMessage='DNS server address(es)')]
    [ValidateNotNullOrEmpty()]
    [string[]]$DnsServer
)

Set-StrictMode -Version Latest

Import-Module CloneHyper-V -Force


Function Get-ArchitectureName() {
    <#
    .SYNOPSIS
    Gets hardware architecture name.

    .DESCRIPTION
    Gets hardware architecture name based on the architecture of the processor.

    .PREREQUISITES
    None.

    .PARAMETER Architecture
    Architecture value.

    .EXAMPLE
    Get-ArchitectureName
    #>
    [CmdletBinding()]
    [OutputType([string])]
    Param(
        [Parameter(Mandatory=$true, HelpMessage='Architecture')]
        [ValidateNotNullOrEmpty()]
        [ValidateRange(0,12)]
        [Uint32]$Architecture
    )

    $name = 'unknown'

    switch ($Architecture) {
        0 { $name = 'x86'; break }
        1 { $name = 'Alpha'; break }
        2 { $name = 'MIPS'; break }
        3 { $name = 'PowerPC'; break }
        5 { $name = 'ARM'; break }
        6 { $name = 'Itanium'; break }
        9 { $name = 'x64'; break }
        12{ $name = 'ARM64'; break }
        default { $name = 'unknown' }
    }

    return $name
}

Function Get-HardwareArchitectureName() {
    <#
    .SYNOPSIS
    Gets hardware architecture name.

    .DESCRIPTION
    Gets hardware architecture name based on the architecture of the processor.

    .PREREQUISITES
    None.

    .EXAMPLE
    Get-HardwareArchitectureName
    #>
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    Param()

    # TODO eliminate WMI if possible due to its slowness
    $processor = Get-WmiObject -Class 'Win32_Processor' -Filter "DeviceID='CPU0'" | Select-Object 'Architecture'
    $architecture = $processor.Architecture
    $name = Get-ArchitectureName -Architecture $architecture

    return $name
}

Function Get-OperatingSystemArchitectureName() {
    <#
    .SYNOPSIS
    Gets operating system architecture name.

    .DESCRIPTION
    Gets operating system architecture name.

    .PREREQUISITES
    Windows XP x86/x64 and later, Windows Server 2003 x86/x64 and later.

    .EXAMPLE
    Get-OperatingSystemArchitectureName
    #>
    [CmdletBinding()]
    [OutputType([string])]
    Param()
    Begin {
        if ($null -eq ([System.Management.Automation.PSTypeName]'Kernel32.NativeMethods').Type) {
            # moved to global scope so wouldn't have to define a new class for every function that does P\Invoke
            # otherwise would get type already exists error when calling different functions that do P\Invoke due to NativeMethods already existing
            $type = @'
        using System.Runtime.InteropServices;
        using System;

        namespace Kernel32 {

            [StructLayout(LayoutKind.Explicit)]
            public struct PROCESSOR_INFO_UNION {
                [FieldOffset(0)]
                public UInt32 dwOemId;
                [FieldOffset(0)]
                public UInt16 wProcessorArchitecture;
                [FieldOffset(2)]
                public UInt16 wReserved;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct SYSTEM_INFO {
                public PROCESSOR_INFO_UNION uProcessorInfo;
                public UInt32 dwPageSize;
                public IntPtr lpMinimumApplicationAddress;
                public IntPtr lpMaximumApplicationAddress;
                public UIntPtr dwActiveProcessorMask;
                public UInt32 dwNumberOfProcessors;
                public UInt32 dwProcessorType;
                public UInt32 dwAllocationGranularity;
                public UInt16 wProcessorLevel;
                public UInt16 wProcessorRevision;
            }

            public class NativeMethods {
                [DllImport("kernel32.dll")]
                public static extern void GetNativeSystemInfo([MarshalAs(UnmanagedType.Struct)] ref SYSTEM_INFO lpSystemInfo);

            }
        }
'@

            Add-Type $type
        }
    }
    Process {

        # unintuitive but returns correct results for OS architecture in a virtualized environment

        $systemInfo = New-Object Kernel32.SYSTEM_INFO
        $systemInfo.uProcessorInfo = New-Object Kernel32.PROCESSOR_INFO_UNION
        [Kernel32.NativeMethods]::GetNativeSystemInfo([ref] $systemInfo)
        $architecture = $systemInfo.uProcessorInfo.wProcessorArchitecture

        $name = Get-ArchitectureName -Architecture $architecture

        return $name
    }
}

Function Get-HardwareBitness() {
    <#
    .SYNOPSIS
    Gets hardware bitness.

    .DESCRIPTION
    Gets hardware bitness.

    .PREREQUISITES
    None.

    .EXAMPLE
    Get-HardwareBitness
    #>
    [CmdletBinding()]
    [OutputType([Uint32])]
    Param()

    # TODO eliminate WMI if possible due to its slowness
    $processor = Get-WmiObject -Class 'Win32_Processor' -Filter "DeviceID='CPU0'" | Select-Object 'DataWidth'
    $bitness = $processor.DataWidth

    return $bitness
}

Function Get-OperatingSystemBitness() {
    <#
    .SYNOPSIS
    Gets operating system bitness.

    .DESCRIPTION
    Gets operating system bitness.

    .PREREQUISITES
    None.

    .EXAMPLE
    Get-OperatingSystemBitness
    #>
    [CmdletBinding()]
    [OutputType([Uint32])]
    Param()

    # TODO eliminate WMI if possible due to its slowness
    $processor = Get-WmiObject -Class 'Win32_Processor' -Filter "DeviceID='CPU0'" | Select-Object 'AddressWidth'
    $bitness = $processor.AddressWidth

    return $bitness
}

Function Get-OperatingSystemReleaseId() {
    <#
    .SYNOPSIS
    Gets the operating system release identifier.

    .DESCRIPTION
    Gets the Windows 10 operating system release identifier (e.g. 1507, 1511, 1607).

    .PREREQUISITES
    Windows 10 x86/x64 and later.

    .EXAMPLE
    Get-OperatingSystemReleaseId
    #>
    [CmdletBinding()]
    [OutputType([UInt32])]
    Param()

    $release = [UInt32](Get-ItemProperty -Path 'HKLM:\Software\Microsoft\Windows NT\CurrentVersion' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty 'ReleaseId' -ErrorAction SilentlyContinue)

    return $release
}

Function Get-OperatingSystemEdition() {
    <#
    .SYNOPSIS
    Gets the operating system edition.

    .DESCRIPTION
    Gets the operating system edition.

    .PREREQUISITES
    Windows 7 x86/x64 and later.

    .EXAMPLE
    Get-OperatingSystemEdition
    #>
    [CmdletBinding()]
    [OutputType([string])]
    Param()

    $edition = Get-ItemProperty -Path 'HKLM:\Software\Microsoft\Windows NT\CurrentVersion' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty 'EditionID' -ErrorAction SilentlyContinue

    return $edition.Trim()
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

Function Get-OperatingSystemName() {
    [CmdletBinding()]
    [OutputType([System.Version])]
    Param()

    $os = Get-WmiObject -Class "Win32_OperatingSystem" -Filter "Primary=true" | Select-Object Caption,OtherTypeDescription
    $osName = $os.Caption -replace "$([char]0x00A9)","" -replace "$([char]0x00AE)","" -replace "$([char]0x2122)","" # remove copyright, registered, and trademark symbols
    $osName = $osName -replace "\(R\)","" -replace "\(TM\)","" # (R) is used on Windows XP X64

    # Windows Server 2003 R2 uses this to signify R2 versus non-R2
    if($null -ne $os.OtherTypeDescription) {
        $osName = ($osName,$os.OtherTypeDescription -join " ")
    }

    return $osName.Trim()
}

Function Wait-VMShutdown() {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true, HelpMessage='VM name')]
        [ValidateNotNullOrEmpty()]
        [string]$Name,

        [Parameter(Mandatory=$false, HelpMessage='The number of seconds to wait (defaults to 1 second) between checking the VM state')]
        [ValidateRange(1,3600)]
        [int]$Frequency = 1,

        [Parameter(Mandatory=$false, HelpMessage='The total maximum number of seconds to wait')]
        [int]$Total = 300
    )

    $count = 0

    do {
        if ($count -ge $Total) {
            break
        } else {
            $count++
        }

        Start-Sleep -Seconds $Frequency

        $service = Get-VM -Name $Name | Get-VMIntegrationService -Name 'VSS'
        $status = $service.PrimaryStatusDescription
        Write-Verbose -Message ('Waiting for VM to shutdown: {0} {1}' -f $count,$status)
    } while ($status -eq 'OK')
}


Function Wait-VMStart() {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true, HelpMessage='VM name')]
        [ValidateNotNullOrEmpty()]
        [string]$Name,

        [Parameter(Mandatory=$false, HelpMessage='The number of seconds to wait (defaults to 1 second) between checking the VM state')]
        [ValidateRange(1,3600)]
        [int]$Frequency = 1,

        [Parameter(Mandatory=$false, HelpMessage='The total maximum number of seconds to wait')]
        [int]$Total = 300
    )

    $count = 0
    #$consecutive = 0

    do {
        if ($count -ge $Total) {
            break
        } else {
            $count++
        }

        $service = Get-VM -Name $Name | Get-VMIntegrationService -Name 'VSS'
        $status = $service.PrimaryStatusDescription

        Start-Sleep -Seconds $Frequency

        #if ($status -eq $service.PrimaryStatusDescription) {
        #    $consecutive++
        #} else {
        #    $consecutive = 0
        #}

        Write-Verbose -Message ('Waiting for VM to start: {0} {1}' -f $count,$status)
    } while ($status -ne 'OK')
}

Function Wait-VMSession() {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true, HelpMessage='VM name')]
        [ValidateNotNullOrEmpty()]
        [string]$Name,

        [Parameter(Mandatory=$false, HelpMessage='The number of seconds to wait (defaults to 1 second) between checking the VM session')]
        [ValidateRange(1,3600)]
        [int]$Frequency = 1,

        [Parameter(Mandatory=$false, HelpMessage='The total maximum number of seconds to wait')]
        [int]$Total = 300,

        [Parameter(Mandatory=$true, HelpMessage='VM credential')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCredential]$Credential
    )

    $count = 0

    do {
        Get-PSSession | Remove-PSSession

        if ($count -ge $Total) {
            break
        } else {
            $count++
        }

        Start-Sleep -Seconds $Frequency
        $session = New-PSSession -VMName $vmTargetName -Credential $Credential;
        Write-Verbose -Message ('Waiting for a valid VM session. {0} {1} {2}' -f $count,$session.State,$session.Availability)
    } while($session.State -ne 'Opened' -and $session.Availability -ne 'Available')
    # moves from Open/Available to Broken/None on shutdown

    Get-PSSession | Remove-PSSession
}

Function Move-Mouse() {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$false, HelpMessage='The number of seconds to wait (defaults to 60 seconds) between checking the VM state')]
        [ValidateRange(1,3600)]
        [int]$Frequency = 60,

        [Parameter(Mandatory=$false, HelpMessage='The total maximum number of seconds to wait')]
        [int]$Total = 3600
    )
    Begin {
        Add-Type -AssemblyName System.Windows.Forms
    }
    Process {
        $count = 0

        while($true) {
            if ($count -ge $Total) {
                break
            }

            $position = [System.Windows.Forms.Cursor]::Position
            [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point -ArgumentList (($position.X) + 1),($position.Y)
            $position = [System.Windows.Forms.Cursor]::Position
            [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point -ArgumentList (($position.X) - 1),($position.Y)

            Start-Sleep -Second $Frequency
            $count++
        }
    }
    End {}
}

Function Send-Keys() {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$false, HelpMessage='The number of seconds to wait (defaults to 60 seconds) between checking the VM state')]
        [ValidateRange(1,3600)]
        [int]$Frequency = 60,

        [Parameter(Mandatory=$false, HelpMessage='The total maximum number of seconds to wait')]
        [int]$Total = 3600
    )
    Begin {
        Add-Type -AssemblyName System.Windows.Forms
    }
    Process {

        $count = 0

        while($true) {
            if ($count -ge $Total) {
                break
            }

            #[System.Windows.Forms.SendKeys]::SendWait(' ')
            [System.Windows.Forms.SendKeys]::SendWait('{NUMLOCK}')

            Start-Sleep -Second $Frequency
            $count++
        }
    }
    End {
        # clear the command prompt of any characters sent
        #[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
    }
}

Function Invoke-Automation() {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true, HelpMessage='VM NAT network address')]
        [ValidateNotNullOrEmpty()]
        [string]$VMNetwork,

        [Parameter(Mandatory=$true, HelpMessage='VM NAT network subnet length aka prefix size')]
        [int]$VMNetworkPrefixSize,

        [Parameter(Mandatory=$true, HelpMessage='The name of a VM, that exists at the $HostVMPath, that will be cloned')]
        [ValidateNotNullOrEmpty()]
        [string]$VMName,

        [Parameter(Mandatory=$true, HelpMessage='The path on the host where the payload for the VM is stored')]
        [ValidateNotNullOrEmpty()]
        [string]$HostStagingPath,

        [Parameter(Mandatory=$true, HelpMessage='The path on the host where the VM payload results are stored')]
        [ValidateNotNullOrEmpty()]
        [string]$HostResultPath,

        [Parameter(Mandatory=$true, HelpMessage='The path in the VM where the payload is stored')]
        [ValidateNotNullOrEmpty()]
        [string]$VMStagingPath,

        [Parameter(Mandatory=$true, HelpMessage='The path in the VM where the payload results are stored')]
        [ValidateNotNullOrEmpty()]
        [string]$VMResultPath,

        [Parameter(Mandatory=$true, HelpMessage='The path on the host where the PowerShell transcript is stored')]
        [ValidateNotNullOrEmpty()]
        [string]$HostTranscriptPath,

        [Parameter(Mandatory=$false, HelpMessage='Do not delete the cloned virtual machine (useful for debugging)')]
        [ValidateNotNullOrEmpty()]
        [switch]$NoDelete,

        [Parameter(Mandatory=$false, HelpMessage='Disable Windows Update')]
        [ValidateNotNullOrEmpty()]
        [switch]$NoWindowsUpdate,

        [Parameter(Mandatory=$false, HelpMessage='DNS server address(es)')]
        [ValidateNotNullOrEmpty()]
        [string[]]$DnsServer,

        [Parameter(Mandatory=$true, HelpMessage='VM credential')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCredential]$Credential
    )

    $parameters = $PSBoundParameters


    if ($Credential.UserName.Length -eq 0) {
         throw 'Forgot to supply a VM username'
    }

    if ($Credential.Password.Length -eq 0) {
         throw 'Forgot to supply a VM password'
    }

    $isVerbose = $verbosePreference -eq 'Continue'

    $startTime = [DateTime]::Now

    Start-Transcript -Path ('{0}\welm_{1:yyyMMddHHmmss}.txt' -f $HostTranscriptPath,$startTime)

    # 1. configure NAT network on host

    $networkPrefix = $VMNetwork
    $prefixLength = $VMNetworkPrefixSize
    $switchName = 'NAT_Switch_{0}' -f $networkPrefix
    $aliasName = 'vEthernet ({0})' -f $switchName # do not change this as Windows creates the name for you, this needs to match what Windows creates
    $natNetworkName = 'NAT_Network_{0}.0/{1}' -f $networkPrefix,$prefixLength
    $natGatewayAddress = '{0}.1' -f $networkPrefix
    $natNetwork = '{0}.0/{1}' -f $networkPrefix,$prefixLength

    if (@(Get-VMSwitch -Name $switchName -ErrorAction SilentlyContinue ).Count -eq 0) {
        New-VMSwitch -SwitchName $switchName -SwitchType Internal
        Write-Verbose -Message ('Created VM switch {0}' -f $switchName)
    }

    $hostInterfaceIndex = (Get-NetAdapter -Name $aliasName).ifIndex

    if (@(Get-NetIPAddress -IPAddress $natGatewayAddress -InterfaceAlias $aliasName -InterfaceIndex $hostInterfaceIndex).Count -eq 0) {
        New-NetIPAddress -IPAddress $natGatewayAddress -PrefixLength $prefixLength -InterfaceAlias $aliasName -InterfaceIndex $hostInterfaceIndex
        Write-Verbose -Message ('Created NAT gateway address {0}' -f $natGatewayAddress)
    }

    if (@(Get-NetNat -Name $natNetworkName -ErrorAction SilentlyContinue).Count -eq 0) {
        # you can only have 1 NAT network, so if you get an exception with error 87, then run Get-NetNat | Remove-NetNat
        New-NetNat -Name $natNetworkName -InternalIPInterfaceAddressPrefix $natNetwork
        Write-Verbose -Message ('Created NAT {0}' -f $natNetworkName)
    }

    # 2. clone the base VM so we can run WELM in the clone

    $vmDirectoryPath = ((Get-VMHost).VirtualMachinePath)

    $vmSourceName = $VMName
    $modifier = 'WELM'
    $vmTargetName = '{0} - {1}' -f $vmSourceName,$modifier

    if (@(Get-VM -Name $vmTargetName -ErrorAction SilentlyContinue).Count -gt 0) {
        throw "$vmTargetName already exists"
    }

    New-VmClone -VMSourceName $vmSourceName -VmTargetName $vmTargetName -PathDirectoryVMStorage $vmDirectoryPath -PathDirectoryTemporaryData "$vmDirectoryPath\Temp" -RemoveTemporaryData -Verbose:$isVerbose

    Remove-Item -Path "$vmDirectoryPath\Temp" -Force -Recurse

    if (@(Get-VM -Name $vmTargetName -ErrorAction SilentlyContinue).Count -eq 0) {
        Remove-Item -Path "$vmDirectoryPath\$vmTargetName" -Force -Recurse -ErrorAction SilentlyContinue
        throw "$vmTargetName was not created"
    }

    if (@(Get-VM -Name $vmTargetName).Count -gt 1) {
        throw "Multiple VMs with the same name of $vmTargetName exist"
    }

    Write-Verbose -Message ('Cloned {0} to {1}' -f $vmSourceName,$vmTargetName)

    # 3. Prepare VM enviroment

    Connect-VMNetworkAdapter -VMName $vmTargetName -SwitchName $switchName

    Write-Verbose -Message ('Connected switch {0} to VM' -f $switchName)

    Start-VM -Name $vmTargetName -Verbose:$isVerbose

    Write-Verbose -Message 'Starting VM'

    Wait-VMStart -Name $vmTargetName -Verbose:$isVerbose

    # Start-VM silently failed because the cloned VM had a path to an ISO file as its CD/DVD drive and the VM service did not have permission to access where it was stored
    if ((Get-VM -Name $vmTargetName).State -ne 'Running') {
        throw "Could not start VM $vmTargetName"
    }

    Write-Verbose -Message 'Started VM'

    Wait-VMSession -Name $vmTargetName -Credential $Credential -Verbose:$isVerbose

    $session = New-PSSession -VMName $vmTargetName -Credential $Credential

    if ($null -eq $session) {
        throw 'Unable to connect to VM. Ensure credentials are correct.'
    }

    # update cloned VM's IP address to a new address to prevent an IP address conflict with the template VM

    $vmInterfaceIndex = Invoke-Command -Session $session -ScriptBlock { (Get-NetAdapter).ifIndex }

    $currentVMAddress = Invoke-Command -Session $session -ScriptBlock { (Get-NetIPAddress -InterfaceIndex $Using:vmInterfaceIndex).IPAddress }

    Invoke-Command -Session $session -ScriptBlock { Remove-NetIPAddress �InterfaceIndex $Using:vmInterfaceIndex �IPAddress $Using:currentVMAddress -Confirm:$false }

    $newVMAddress = '{0}.{1}' -f $networkPrefix,(Get-Random -Minimum 20 -Maximum 250)

    Invoke-Command -Session $session -ScriptBlock { New-NetIPAddress �InterfaceIndex $Using:vmInterfaceIndex �IPAddress $Using:newVMAddress -PrefixLength $Using:prefixLength }

    $updatedVMAddress = Invoke-Command -Session $session -ScriptBlock { (Get-NetIPAddress -InterfaceIndex $Using:vmInterfaceIndex).IPAddress }

    if ($updatedVMAddress -ne $newVMAddress) {
        throw 'VM address was not updated'
    }

    Write-Verbose -Message ('Set VM IP address. Old address: {0} New address: {1} Interface index: {2}' -f $currentVMAddress,$newVMAddress,$vmInterfaceIndex)

    # update gateway if needed

    $routes = Invoke-Command -Session $session -ScriptBlock { (Get-NetRoute -InterfaceIndex $Using:vmInterfaceIndex) }

    $gateway = $routes | Where-Object { $_.DestinationPrefix -eq '0.0.0.0/0' }

    if ($null -eq $gateway) {
        Invoke-Command -Session $session -ScriptBlock { New-NetRoute -DestinationPrefix '0.0.0.0/0' -NextHop $Using:natGatewayAddress -InterfaceIndex $Using:vmInterfaceIndex }
    } else {
        if ($gateway.NextHop -ne $natGatewayAddress) {
            Invoke-Command -Session $session -ScriptBlock { Remove-NetRoute -DestinationPrefix '0.0.0.0/0' -InterfaceIndex $Using:vmInterfaceIndex }
            Invoke-Command -Session $session -ScriptBlock { New-NetRoute -DestinationPrefix '0.0.0.0/0' -NextHop $Using:natGatewayAddress -InterfaceIndex $Using:vmInterfaceIndex }
        }
    }

    # update DNS servers if needed

    if ($parameters.ContainsKey('DnsServer')) {
        #todo: check DNS servers first and only update if needed

        Invoke-Command -Session $session -ScriptBlock { Set-DNSClientServerAddress -InterfaceIndex $Using:vmInterfaceIndex -ServerAddresses $using:DnsServer}

        $currentDnsServer = Invoke-Command -Session $session -ScriptBlock { Get-DNSClientServerAddress -InterfaceIndex $Using:vmInterfaceIndex}

        Write-Verbose -Message ('Set VM DNS address. {0}' -f ($currentDNSServer -join ','))
    }

    if($NoWindowsUpdate) {
        Invoke-Command -Session $session -ScriptBlock { Set-ItemProperty -Path 'hklm:\Software\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update' -Name 'NoAutoUpdate' -Value 1 -Type 'DWORD'
        Invoke-Command -Session $session -ScriptBlock { Set-ItemProperty -Path 'hklm:\Software\Policies\Microsoft\Windows\WindowsUpdate\AU' -Name 'NoAutoUpdate' -Value 1 -Type 'DWORD' }}
    }

    $osBitness = Invoke-Command -Session $session -ScriptBlock ${function:Get-OperatingSystemBitness}

    #todo: set proxy based on bitness

    Invoke-Command -Session $session -ScriptBlock { New-Item -Path $Using:VMStagingPath -ItemType Directory -Force | Out-Null }
    Invoke-Command -Session $session -ScriptBlock { New-Item -Path $Using:VMResultPath -ItemType Directory -Force | Out-Null }

    Write-Verbose -Message 'Created folders in VM'

    $vmWelmPath = "$VMStagingPath\welm"

    Copy-Item -ToSession $session -Path $HostStagingPath -Destination $vmWelmPath -Recurse

    Write-Verbose -Message ('Started feature install in VM at {0}' -f [System.DateTime]::Now)

    Invoke-Command -Session $session -ScriptBlock { Start-Process -FilePath 'powershell.exe' -ArgumentList '-Command `"& { Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope LocalMachine }`"'}

    Remove-PSSession -Session $session

    $preventSleepJob = Start-Job -ScriptBlock ${function:Send-Keys} -ArgumentList 15,900

    do {
        $session = New-PSSession -VMName $vmTargetName -Credential $Credential

        Invoke-Command -Session $session -FilePath "$HostStagingPath\Install-Features.ps1" # $Using:HostStagingPath is not valid in the FilePath case
        $results = Invoke-Command -Session $session -ScriptBlock { Invoke-InstallFeatures -Verbose:$Using:isVerbose }

        Write-Verbose -Message ('Installed features in VM at {0}. Needs reboot: {1} Needs more features installed: {2}' -f [System.DateTime]::Now,$results.NeedsReboot,$results.NeedsInstall)

        # workaround transcript not capturing Invoke-InstallFeatures verbose output
        Write-Verbose -Message ('{0}{1}' -f [Environment]::NewLine,$results.Log)

        if ($results.NeedsReboot){
            Invoke-Command -Session $session -ScriptBlock { Restart-Computer -Force }

            Wait-VMShutdown -Name $vmTargetName

            Write-Verbose -Message 'Shutdown VM'

            if((Get-VM -Name $vmTargetName).State -ne 'Running') {
                Start-VM -Name $vmTargetName
            }

            Write-Verbose -Message 'Starting VM'

            Wait-VMStart -Name $vmTargetName

            Wait-VMSession -Name $vmTargetName -Credential $Credential

            if ((Get-VM -Name $vmTargetName).State -ne 'Running') {
                throw "Could not start VM $vmTargetName"
            }

            Write-Verbose -Message 'Started VM'
        }

        Get-PSSession | Remove-PSSession

    } while ($results.NeedsInstall)

    Write-Verbose -Message ('Finished feature install in VM at {0}' -f [System.DateTime]::Now)

    Wait-VMSession -Name $vmTargetName -Credential $Credential

    Write-Verbose -Message 'Waiting to allow time for VM to finish installing features'
    Start-Sleep -Seconds 180 # unavoidable, ensure all the features have fully installed after reboot

    $session = New-PSSession -VMName $vmTargetName -Credential $Credential

    #Invoke-Command -Session $session -ScriptBlock { Start-Process -FilePath 'cmd.exe' -ArgumentList '/c','C:\transfer\in\welm\welm.bat' -Wait }

    Write-Verbose -Message ('Started WELM at {0}' -f [System.DateTime]::Now)
    Invoke-Command -Session $session -ScriptBlock { Start-Process -FilePath ('{0}\welm.bat' -f $Using:vmWelmPath) -WorkingDirectory $Using:vmWelmPath -Wait } # WorkingDirectory is critical, otherwise execution context for welm.exe (int .bat file) is at C:\users\user\Documents in the VM
    Write-Verbose -Message ('Finished WELM at {0}' -f [System.DateTime]::Now)

    Stop-Job -Job $preventSleepJob

    Invoke-Command -Session $session -ScriptBlock { Remove-Item ('{0}\*.exe' -f $Using:vmWelmPath) }
    Invoke-Command -Session $session -ScriptBlock { Remove-Item ('{0}\*.bat' -f $Using:vmWelmPath) }
    Invoke-Command -Session $session -ScriptBlock { Remove-Item ('{0}\*.ps1' -f $Using:vmWelmPath) }
    Invoke-Command -Session $session -ScriptBlock { Remove-Item ('{0}\*.config' -f $Using:vmWelmPath) }


    # since Get-OperatingSystemArchitectureName depends on another custom function called called Get-ArchitectureName, we have to include both functions before the call to Invoke-Command
    $functionDefinitions = "Function Get-ArchitectureName { ${function:Get-ArchitectureName} }; Function Get-OperatingSystemArchitectureName { ${function:Get-OperatingSystemArchitectureName} }"

    $osArchName = Invoke-Command -Session $session -ScriptBlock {
        . ([ScriptBlock]::Create($Using:functionDefinitions))
        Get-OperatingSystemArchitectureName
    }

    # remaining functions have no custom dependent functions
    $osEdition = Invoke-Command -Session $session -ScriptBlock ${function:Get-OperatingSystemEdition}
    $osRelease = Invoke-Command -Session $session -ScriptBlock ${function:Get-OperatingSystemReleaseId}
    $osVersion = Invoke-Command -Session $session -ScriptBlock ${function:Get-OperatingSystemVersion}
    $osName = Invoke-Command -Session $session -ScriptBlock ${function:Get-OperatingSystemName}

    $osName = $osName.Replace('Microsoft','').Replace($osEdition,'').Trim().Replace(' ','_')

    $timestamp = '{0:yyyMMddHHmmss}' -f [System.DateTime]::Now

    $file = ('{0}_{1}_{2}_{3}_{4}_{5}.zip' -f $osName,$osRelease,$osEdition,$osArchName,$osVersion,$timestamp).ToLower()

    Invoke-Command -Session $session -ScriptBlock { Compress-Archive -Path ('{0}\*' -f $Using:vmWelmPath) -DestinationPath ('{0}\{1}' -f $Using:VMResultPath,$Using:file) }

    Copy-Item -FromSession $session -Path ('{0}\{1}' -f $VMResultPath,$file) -Destination $HostResultPath -Force

    $filePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($file)

    Write-Verbose -Message ('Copied WELM data to {0}' -f $filePath)

    Remove-PSSession $session

    Stop-VM -Name $vmTargetName -Force | Out-Null

    Wait-VMShutdown -Name $vmTargetName

    Write-Verbose -Message 'Stopped VM'

    if(-not($NoDelete)) {
        Remove-VM -Name $vmTargetName -Force

        # prompts in ISE
        #Remove-Item -Path "$vmDirectoryPath\$vmTargetName" -Recurse -Confirm:$false -ErrorAction SilentlyContinue

        Get-ChildItem -Path "$vmDirectoryPath\$vmTargetName" -Recurse | Remove-Item -Recurse -Force -Confirm:$false
        Remove-Item -Path "$vmDirectoryPath\$vmTargetName" -Force -Confirm:$false

        Write-Verbose -Message 'Deleted VM'
    }

    $endTime = [DateTime]::Now

    Write-Verbose -Message ('Start: {0} End: {1}' -f $startTime,$endTime)

    Stop-Transcript
}

Invoke-Automation @PSBoundParameters -Credential (Get-Credential)