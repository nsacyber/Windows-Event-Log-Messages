# Usage and examples
~~~
    Usage:
        welm.exe -h 
        welm.exe ([-p | -l | -e]) -f Format

    Options:
        -h --help                   Shows this dialog.
        -p --providers              Retrieve all providers.
        -l --logs                   Retrieve all logs.
        -e --events                 Retrieve all events.
        -f Format, --format Format  Specify format. txt, json, csv, or all.
~~~


Running **welm.exe -p -f all** will retrieve all provider data in all supported formats. 

Running **welm.exe -l -f all** will retrieve all log data in all supported formats. 

Running **welm.exe -e -f all** will retrieve all event data in all supported formats. 

Instead of using **all**, you can retrieve data in specific formats:
* **-f txt**
* **-f json**
* **-f csv**

# Output
Running WELM results in the following files being generated:

* **classicevents.txt**/**.json**/**.csv** - Contains metadata for classic style events. Available for Windows XP and later.
* **classiclogs.txt**/**.json**/**.csv** - Contains metadata for classic style logs. Available for Windows XP and later.
* **classicsources.txt**/**.json**/**.csv** - Contains metadata for classic style event sources. Available for Windows XP and later.
* **events.txt**/**.json**/**.csv** - Contains metadata for new style events. Available for Windows Vista and later.
* **logs.txt**/**.json**/**.csv** - Contains metadata for new style logs. Available for Windows Vista and later.
* **providers.txt**/**.json**/**.csv** - Contains metadata for new style providers. Available for Windows Vista and later.
* **welm**.***yyyyMMddHHmms***\_***loglevel***.**txt** - Log files useful for observing WELM's internal operations.

**"Classic style" versus "new style"**. The "new style" metadata is retrieved using Windows event log APIs introduced in Windows Vista  with the new event log system often referred to by its codename of Crimson. The "classic style" metadata is retrieved from the Windows registry for log metadata and source metadata and from text resources embedded in binaries for event metadata. 

Some of the classic style events are just normal strings (UI text elements, etc) that are embedded in the binary. Unfortunately there isn't a reliable way to differentiate between an event text string and a normal text string (that is used for other uses) that doesn't result in losing legitimate event text strings. The new style event data does not have this problem.

# Running WELM

Once you have [built WELM](./Building WELM.md), copy the files from the dist folder to a virtual machine. Install all features and roles in the operating system before running WELM to get the most complete output. This document describes how to do that for each major Windows operating system release starting with Windows XP.

WELM requires administrative rights to retrieve all event information. Specifically it needs administrative rights to get event information about the Security log, its related providers, and its events.

## Retrieving data

Below are the generic steps for retrieving data with WELM.

1. Create virtual machines for the x86 and x64 versions of the operating system. Use Enterprise Edition since it has features that other editions of Windows do not.
1. Install WELM prerequisites (.Net 4.0) in the virtual machine for any OSes that don't have it (Windows 8.1 and earlier).
1. Enable all features in Windows either manually (XP, 2003, Vista, Server 2008) or with the provided Install-Features.ps1 PowerShell script (Windows 7/Server 2008 R2 and later). 
1. On server editions of the operating system, install all roles and run dcpromo to allow retrieving of all possible events.
1. Copy welm.exe, welm.bat, NLog.config, and Install-Features.ps1 to the virtual machine.
1. Open a PowerShell administrator prompt, dot source (e.g. **. .\\Install-Features.ps1**) the [Install-Features.ps1](..\welm\Install-Features.ps1) script, and then run **Invoke-InstallFeatures -Verbose**
1. Open an administrator command prompt and run welm.bat.
1. Copy the generated data out of the virtual machine.
1. Run **New-Statistics -Path 'C:\path to folder containing retrieved data'** from the Get-Statistics.ps1 file to generate statistics files based on the data.

If using a Windows 10/Windows Server 2016 host and guest OS, then you can use the [Automate-WELM.ps1](..\welm\Automate-WELM.ps1] script to perform all tasks once a virtual machine has been created.

You can find operating system specific instructions below:

* [Windows XP SP3](#windows-xp)
* [Windows Server 2003 R2 SP1](#windows-server-2003)
* [Windows Vista SP2/Windows Server 2008 R2](#windows-vistawindows-server-2008)
* [Windows 7/Windows Server 2008 R2](#windows-7windows-server-2008-r2)
* [Windows 8/Windows Server 2012](#windows-8windows-server-2012)
* [Windows 8.1/Windows Server 2012 R2](#windows-81windows-server-2012-r2)
* [Windows 10 1507 and later/Windows Server 2016](#windows-10-1507-and-laterwindows-server2016)


### Windows XP

1. Create a VM
1. Take a snapshot
1. Right click the avhdx file and mount it.
1. Copy the .Net 3.5 SP1 installer (dotnetfx35sp1full.exe) from into the mounted image.
1. Copy \WELM into the mounted image.
1. Right click the avhdx file and unmount it.
1. Right click on the VM in Hyper-V and select Settings. Under IDE Controller 1 where it says DVD Drive, select the Image File option in the right pane. Click the Browse button and browse to the ISO and click Open. Click OK.
1. Start the VM. In the VM go to Start > Settings > Control Panel. Click Switch to Classic View. Select Add or Remove Programs. Click Add/Remove Windows Components. Select all the components and click Next. Browse to the drive that was just assigned the XP SP3 image to. Click Finish.
1. Open a command prompt as Administrator and run welm.bat
1. Shutdown the VM.
1. Mount the avhdx file. Copy the output (classicevents.*, classiclogs.*, classicsources.*, *.log) to to the local system. Unmount the avhdx file.


Windows XP 64-bit needed file system redirection temporarily disabled in some cases since sysnative does not exist until Vista. Could've installed KB942859 and see what happens but P\Invoked Wow64DisableWow64FsRedirection and Wow64RevertWow64FsRedirection instead.

### Windows Server 2003

Same as Windows XP steps. Put junk values in everything when components are installed. Don't install Certificate Services until after running dcpromo because it will ask to uninstall it. Right click on the VM and give it a valid network adapter and connection. This is temporarily required mainly for dcpromo but it helps for IIS too.

If a Windows feature/component is checked but its box is gray rather than white, then that means sub-components are not getting installed. Select the component and click the Details button to enable all the components. Some components that won't be installed:

* Active Directory Services > ADFS > 
    * Federation Service - requires dcpromo AND a SSL certificate in IIS
    * Federation Proxy - requires dcpromo AND a SSL certificate in IIS
* Active Directory Services > Indentity Management for UNIX >
    * Server for NIS - requires dcpromo
* Application Server
    * Enable network DTC access - wouldn't install on x86, but did on x64
* Application Server > Message Queuing >
    * Downlevel Client Support - requires running dcpromo... wouldn't install on x86, complained about MSMQ 1.0 being installed. Installed on x64.
    * Routing Support - requires running dcpromo... wouldn't install on x86, complained about MSMQ 1.0 being installed. Installed on x64.

The ADFS service and proxy wouldn't install on x65. On x86 there was a bunch of other components that wouldn't install as outlined above.

Run dcpromo.

Install Certificate Services. Put junk values in everything except the Distinguished Name during CA installation. Use C=test as an example value.

Also install the other parts as mentioned above (Server for NIS, Application Server stuff).

Run welm.

### Windows Vista/Windows Server 2008

1. While it is possible to script setup with pkgmgr and/or ocsetup, they are not ideal. It is much faster to select all the features manually in the UI. Make sure all items have a check mark rather than a filled in square.

1. Start > Settings > Control Panel > Programs > Turn Windows Features on or off. On the server it brings up the Roles and Features dialog. Install all the features possible and reboot. There will only be two sub-items that can't install: Message Queuing > Message Queuing Services > Routing Support and Message Queuing > Windows 2000 Client Support.
1. Install .Net 3.5.
1. Install all roles except for Active Directory ones. WSUS won't install from Server Manager. Install WSUS 3.0 SP2 (SP1 is what would normally come with Server 2008 so SP2 is good enough).
1. Install Active Directory Domain Services. Run dcpromo.
1. Install the remaining AD roles (create a user in AD for AD RMS to install and use the server's fqdn as the RMS federation server name and cluster server name).
1. Go back to Add Features and install Message Queuing > Message Queuing Services > Routing Support.  The other sub item (Message Queuing > Windows 2000 Client Support)  will not install.
1. Run welm.


Can't install Hyper-V on x64 while already inside a Hyper-V VM. Server 2008 SP2 has to be installed on physical hardware in order to get those logs.

### Windows 7/Windows Server 2008 R2

Net 3.5 is already installed by default on Windows 7. 

1. Set-ExecutionPolicy Unrestricted -Force
1. Run the install all features script and reboot.

On server add roles.

The install all features script causes too many issues on Server 2008 R2 SP1. Installing Hyper-V causes mouse integration to break. Some features don't get installed (Application Server) either. Install everything manually like how it was done for Server 2008 SP2 to get the most features installed. Manually install WSUS. After dcpromo, add remaining features which results in all features/sub-features getting installed. Remove Remote Server Administration Tools > Role Administration Tools > AD and AD LDS Tools > AD DS Tools > Server for NIS Tools. This is so the Server for NIS role service under the AD DS role can be installed. Add role services to AD CS, AD DS, Print and Document Services, Remote Desktop Services.

Can't install ADFS > FS Proxy, File Services > Windows Server 2003 File Services, Remove Desktop Services > Remote Desktop Services Virtualization Host, and Hyper-V.


Install remaining features that are listed as Disabled at the very end. Run the script. After reboot everything will have to be done by keyboard but that's easy since all that's left is running welm.bat.

### Windows 8/Windows Server 2012

To install .Net 3.5 on an internet disconnected Windows 8 system, insert the Windows 8 ISO into the virtual drive and then run this command: dism /online /Enable-Feature /FeatureName:NetFx3 /All /LimitAccess /Source:D:\sources\sxs The /Source path will vary based on which drive letter was assigned to the drive that contains the Windows image.

Run the install all features script and reboot.

Check all features are installed. If not, run the script again and reboot.

### Windows 8.1/Windows Server 2012 R2

Same as Windows 8/Server 2012.


Give it a network card in the Hyper-V settings.
In the VM change it to a static IP.
Remove Active Directory Certificate Services' Certification Authority sub option only. It needs to be removed because dcpromo doesn't want it. Do this from the Turn Windows Features on or off
Go to Server Manager > AD DS > click the More... link in the light yellow bar thingy. Click the Promote this server to a domain controller link under the Action column. Follow the steps to create a new domain.
Install the Certification Authority sub option again.


To collect all the events that are possible, run WELM on physical machines for any edition of Windows that supports Hyper-V. For servers, this is obvious. For non-servers, this applies to Windows 8 and later since Enterprise Edition supports client Hyper-V. Can't install Hyper-V into a virtual machine (at least not in a Hyper-V VM). The script appears to install Hyper-V even when it is in a VM.

### Windows 10 1507 and later/Windows Server 2016
Starting with Windows 10 the [Automate-WELM.ps1](..\welm\Automate-WELM.ps1) script is provided to automate retrieval of data generated by WELM. The host must be at least Windows 10 1507 with Hyper-V installed and the guest must be Windows 1507 or later since the script uses PowerShell Direct ([1](https://msdn.microsoft.com/en-us/virtualization/hyperv_on_windows/user_guide/vmsession), [2](https://technet.microsoft.com/en-us/windows-server-docs/compute/hyper-v/manage/manage-windows-virtual-machines-with-powershell-direct)).

Create a template virtual machine that can be cloned by the script. The virtual machine should have internet access OR a Windows installation DVD inserted into its virtual DVD drive. Run the following command on the host as an administrator:

```
powershell.exe -File "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\Automate-WELM.ps1" -VMNetwork 172.16.0 -VMNetworkPrefixSize 24 -VMName "Windows 10 1607 Enterprise x64" -HostStagingPath "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist" -HostResultPath "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\docs\data" -VMStagingPath "C:\transfer\in" -VMResultPath "C:\transfer\out" -HostTranscriptPath "C:\users\user\Desktop" powershell.exe -File "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\Automate-WELM.ps1" -VMNetwork 172.16.0 -VMNetworkPrefixSize 24 -VMName "Windows 10 1607 Enterprise x64" -HostCodePath "C:\users\user\Desktop" -HostStagingPath "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist" -HostResultPath "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\docs\data" -VMStagingPath "C:\transfer\in" -VMResultPath "C:\transfer\out" -HostTranscriptPath "C:\users\user\Desktop" -Verbose
```