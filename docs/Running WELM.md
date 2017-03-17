# Running WELM

WELM requires administrative rights to retrieve all event information. Specifically it needs administrative rights to get event information about the Security log, its related providers, and its events.

## Usage and examples
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


Running **welm.exe -p -f all** retrieves all provider data in all supported formats. 

Running **welm.exe -l -f all** retrieves all log data in all supported formats. 

Running **welm.exe -e -f all** retrieves all event data in all supported formats. 

Instead of using **all**, you can retrieve data in specific formats:
* **-f txt**
* **-f json**
* **-f csv**

## Output
Running WELM results in the following files being generated:

* **classicevents.txt**/**.json**/**.csv** - Contains metadata for classic style events. Available for Windows XP and later.
* **classiclogs.txt**/**.json**/**.csv** - Contains metadata for classic style logs. Available for Windows XP and later.
* **classicsources.txt**/**.json**/**.csv** - Contains metadata for classic style event sources. Available for Windows XP and later.
* **events.txt**/**.json**/**.csv** - Contains metadata for new style events. Available for Windows Vista and later.
* **logs.txt**/**.json**/**.csv** - Contains metadata for new style logs. Available for Windows Vista and later.
* **providers.txt**/**.json**/**.csv** - Contains metadata for new style providers. Available for Windows Vista and later.
* **welm**.***yyyyMMddHHmms***\_***loglevel***.**txt** - Log files useful for observing WELM's internal operations.

**"Classic style" versus "new style"**. The "new style" metadata is retrieved using Windows event log APIs introduced in Windows Vista  with the new event log system often referred to by its codename of Crimson. The "classic style" metadata is retrieved from the Windows registry for log metadata and source metadata and from text resources embedded in binaries for event metadata. 

Some of the classic style events are just normal strings (UI text elements, etc) that are embedded in the binary. Unfortunately there isn't a reliable way to differentiate between an event text string and a normal text string (that's used for other uses) that doesn't result in losing legitimate event text strings. The new style event data does not have this problem.

# Retrieving data
This section describes how to maximize the amount of collected event information.

## Host system preparation

What you need:
1. A compiled debug or release version of WELM (see [Building WELM](./Building WELM.md)).
1. A host system running Windows 10 1607 or later (required for [PowerShell Direct](https://docs.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/powershell-direct) [persistent session](https://docs.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/powershell-direct#copy-files-with-new-pssession-and-copy-item) and NAT switch support).
1. Hyper-V installed on the host system.
1. A Hyper-V [NAT switch](https://docs.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/setup-nat-network) if you want the system to access the Internet to install patches from Windows Update.
1. A .iso file containing installation media for Enterprise Edition (x86 and x64) for desktop systems and Datacenter or Enterprise Edition for server systems.

## Virtual machine preparation

Operating system specific instructions below.

[Desktops](#desktops):
* [Windows XP SP3](#windows-xp)
* [Windows Vista SP2](#windows-vista)
* [Windows 7 SP1](#windows-7)
* [Windows 8](#windows-8)
* [Windows 8.1](#windows-81)
* [Windows 8.1 Update](#windows-81-update)
* [Windows 10 1507](#windows-10-1507)
* [Windows 10 1511](#windows-10-1511) 
* [Windows 10 1607 and later](#windows-10-1607-and-later)

[Servers](#servers):
* [Windows Server 2003 R2 SP1](#windows-server-2003)
* [Windows Server 2008 SP2](#windows-server-2008)
* [Windows Server 2008 R2 SP1](#windows-server-2008-r2)
* [Windows Server 2012](#windows-server-2012)
* [Windows Server 2012 R2](#windows-server-2012-r2)
* [Windows Server 2016](#windows-server-2016)

### Desktops

#### Windows XP

1. Create a virtual machine (VM) and install the operating system using the .iso installation media. DO NOT connect it to a virtual switch. Make sure the .iso installation media is mounted as a DVD drive in the VM settings.
1. Shutdown the VM.
1. Find the .vhdx file on the host system that represents the VM's hard drive.
1. Right click on the .vhdx file and select **Mount**. The drive root should open in Windows Explorer.
1. Browse the path of **\\Documents and Settings\\user\\Desktop\\** of the VM drive (click **Yes** at the security prompt).
1. Copy the **dist** folder from inside the **welm** folder of the GitHub repository folder to the Desktop of the VM drive and close the Windows Explorer window for the VM drive.
1. Browse to **This PC** in Windows Explorer on the host system, right click the drive letter (probably D or E) that corresponds to the VM drive and select **Eject**.
1. Power on the VM.
1. Login to the VM.
1. **Start** > **Control Panel**.
2. Click **Switch to Classic View**.
1. Select **Add or Remove Programs**.
1. Click **Add/Remove Windows Components**.
1. Make sure all features have a check mark with a white background next to them rather than check mark with a gray background. If the background is gray, then select the feature and click the **Details** button. Repeat this for all features AND sub-features.
1. Click **Next**.
1. Click **Finish**.
1. Reboot the system.
1. Install .Net 4.0.
1. Open an administrator command prompt.
1. Type **cd C:\Documents and Settings\user\Desktop\dist\release** and press **Enter**. 
1. Type **welm.bat** and press **Enter**. It should take 10-15 minutes to complete.
1. Open one of the welm.yyyyMMddHHmms_info.txt log files and copy the WELM ID from the third line of the file. Create a new folder on the Desktop using the WELM ID as the name.
1. Copy the contents of the **welm** folders, that is inside the **\\Desktop\\dist\\release\\** folder, to inside the folder named after the WELM ID.
1. Make sure there are no welm.yyyyMMddHHmms_fatal.txt log files. If there is, then run welm.bat from the dist\debug\ folder and open an issue in the WELM GitHub repository. Add the contents of the file, or attach it, to the issue.
1. Shutdown the VM.
1. Find the .vhdx file on the host system that represents the VM's hard drive.
1. Right click on the .vhdx file and select **Mount**. The drive root should open in Windows Explorer.
1. Browse the path of **\\Documents and Settings\\user\\Desktop\\** of the VM drive.
1. Copy the WELM ID folder from inside the VM drive to the host system and close the Windows Explorer window for the VM drive. 
1. Browse to **This PC** in Windows Explorer on the host system, right click the drive letter (probably D or E) that corresponds to the VM drive and select **Eject**.

#### Windows Vista

While it may be possible to script installation of all features with pkgmgr and/or ocsetup, it is easy and not very time consuming to select all the features manually in the user interface. 

1. Create a virtual machine (VM) and install the operating system using the .iso installation media. DO NOT connect it to a virtual switch. Make sure the .iso installation media is mounted as a DVD drive in the VM settings.
1. Shutdown the VM.
1. Find the .vhdx file on the host system that represents the VM's hard drive.
1. Right click on the .vhdx file and select **Mount**. The drive root should open in Windows Explorer.
1. Browse the path of **\\users\\user\\Desktop\\** of the VM drive (click **Yes** at the security prompt).
1. Copy the **dist** folder from inside the **welm** folder of the GitHub repository folder to the Desktop of the VM drive and close the Windows Explorer window for the VM drive.
1. Browse to **This PC** in Windows Explorer on the host system, right click the drive letter (probably D or E) that corresponds to the VM drive and select **Eject**.
1. Power on the VM.
1. Login to the VM.
1. **Start** > **Control Panel** > **Programs** > **Turn Windows Features on or off**. 
1. Make sure all features have a check mark next to them rather than a filled in square.
1. Reboot the system.
1. Install .Net 4.0.
1. Open an administrator command prompt.
1. Type **cd C:\\users\\user\\Desktop\\dist\\release** and press **Enter**. 
1. Type **welm.bat** and press **Enter**. It should take 10-15 minutes to complete.
1. Open one of the welm.yyyyMMddHHmms_info.txt log files and copy the WELM ID from the third line of the file. Create a new folder on the Desktop using the WELM ID as the name.
1. Copy the contents of the **wevtutil** and **welm** folders, that are inside the **\\Desktop\\dist\\release\\** folder, to inside the folder named after the WELM ID.
1. Make sure there are no welm.yyyyMMddHHmms_fatal.txt log files. If there is, then run welm.bat from the dist\debug\ folder and open an issue in the WELM GitHub repository and add the contents of the file, or attach it, to the issue.
1. Shutdown the VM.
1. Find the .vhdx file on the host system that represents the VM's hard drive.
1. Right click on the .vhdx file and select **Mount**. The drive root should open in Windows Explorer.
1. Browse the path of **\\users\\user\\Desktop\\** of the VM drive.
1. Copy the WELM ID folder from inside the VM drive to the host system and close the Windows Explorer window for the VM drive. 
1. Browse to **This PC** in Windows Explorer on the host system, right click the drive letter (probably D or E) that corresponds to the VM drive and select **Eject**.

#### Windows 7

Instructions are the same for [Windows 8](#windows-8) except make sure you install the [.Net Framework 4.0](https://www.microsoft.com/en-us/download/details.aspx?id=17718).

The OEMHelpCustomization and CorporateHelpCustomization features fail to install with an error of 1603.

#### Windows 8

1. Create a virtual machine (VM) and install the operating system using the .iso installation media. DO NOT connect it to a virtual switch. Make sure the .iso installation media is mounted as a DVD drive in the VM settings.
1. Shutdown the VM.
1. Find the .vhdx file on the host system that represents the VM's hard drive.
1. Right click on the .vhdx file and select **Mount**. The drive root should open in Windows Explorer.
1. Browse the path of **\\users\\user\\Desktop\\** of the VM drive (click **Yes** at the security prompt).
1. Copy the **dist** folder from inside the **welm** folder of the GitHub repository folder to the Desktop of the VM drive and close the Windows Explorer window for the VM drive.
1. Browse to **This PC** in Windows Explorer on the host system, right click the drive letter (probably D or E) that corresponds to the VM drive and select **Eject**.
1. Power on the VM.
1. Login to the VM.
1. Open an administrator command prompt.
1. Type **cd C:\users\user\Desktop\dist\release** and press **Enter**.
1. Type **powershell.exe** and press **Enter**.
1. Type **Set-ExecutionPolicy Unrestricted** and press **Enter**.
1. Type **. .\Install-Features.ps1** and press **Enter**.
1. Type **$result=Invoke-InstallFeatures -Verbose** and press **Enter**.
1. Once the script has completed, type **$result** and press **Enter**.
1. If **NeedsReboot** is true, then restart the system.
1. If **NeedsInstall** is true, then run the script again after the system reboots. Repeat this until NeedsInstall is false.
1. Reboot the VM if needed and then login to the VM.
1. Open an administrator command prompt.
1. Type **cd C:\users\user\Desktop\dist\release** and press **Enter**. 
1. Type **welm.bat** and press **Enter**. It should take 10-15 minutes to complete.
1. Open one of the welm.yyyyMMddHHmms_info.txt log files and copy the WELM ID from the third line of the file. Create a new folder on the Desktop using the WELM ID as the name.
1. Copy the contents of the **wevtutil** and **welm** folders, that are inside the **\\Desktop\\dist\\release\\** folder, to inside the folder named after the WELM ID.
1. Make sure there are no welm.yyyyMMddHHmms_fatal.txt log files. If there is, then run welm.bat from the dist\debug\ folder and open an issue in the WELM GitHub repository and add the contents of the file, or attach it, to the issue.
1. Shutdown the VM.
1. Find the .vhdx file on the host system that represents the VM's hard drive.
1. Right click on the .vhdx file and select **Mount**. The drive root should open in Windows Explorer.
1. Browse the path of **\\users\\user\\Desktop\\** of the VM drive.
1. Copy the WELM ID folder from inside the VM drive to the host system and close the Windows Explorer window for the VM drive. 
1. Browse to **This PC** in Windows Explorer on the host system, right click the drive letter (probably D or E) that corresponds to the VM drive and select **Eject**. 

#### Windows 8.1

Follow the instructions in the [Windows 8](#windows-8) section. 

#### Windows 8.1 Update

Follow the instructions in the [Windows 8](#windows-8) section. You will need to rename the output folder to differentiate it from Windows 8.1 without the update. Insert \_u\_ between "8.1" and "enterprise"

#### Windows 10 1507

Follow the instructions in the [Windows 8](#windows-8) section. 

#### Windows 10 1511

Follow the instructions in the [Windows 8](#windows-8) section. 

#### Windows 10 1607 and later
Starting with Windows 10 1607, the [Automate-WELM.ps1](..\welm\Automate-WELM.ps1) script is provided to automate retrieval of data generated by WELM. The host must be at least Windows 10 1607 with Hyper-V installed and the guest must be Windows 10 1607 or later since the script uses PowerShell Direct ([1](https://msdn.microsoft.com/en-us/virtualization/hyperv_on_windows/user_guide/vmsession), [2](https://technet.microsoft.com/en-us/windows-server-docs/compute/hyper-v/manage/manage-windows-virtual-machines-with-powershell-direct)).

Create a template virtual machine that can be cloned by the script. The virtual machine should have Internet access OR a Windows installation DVD inserted into its virtual DVD drive. Run the following command on the host as an administrator:

```
powershell.exe -File "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\Automate-WELM.ps1" -VMNetwork 172.16.0 -VMNetworkPrefixSize 24 -VMName "Windows 10 1607 Enterprise x64" -HostStagingPath "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist" -HostResultPath "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\docs\data" -VMStagingPath "C:\transfer\in" -VMResultPath "C:\transfer\out" -HostTranscriptPath "C:\users\user\Desktop" powershell.exe -File "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\Automate-WELM.ps1" -VMNetwork 172.16.0 -VMNetworkPrefixSize 24 -VMName "Windows 10 1607 Enterprise x64" -HostCodePath "C:\users\user\Desktop" -HostStagingPath "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist" -HostResultPath "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\docs\data" -VMStagingPath "C:\transfer\in" -VMResultPath "C:\transfer\out" -HostTranscriptPath "C:\users\user\Desktop" -DnsServer '123.45.67.890' -NoWindowsUpdate -Verbose
```

### Servers

#### Windows Server 2003

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

#### Windows Server 2008
On the server it brings up the Roles and Features dialog. Install all the features possible and reboot. There will only be two sub-items that can't install: Message Queuing > Message Queuing Services > Routing Support and Message Queuing > Windows 2000 Client Support.
1. Install .Net 3.5.
1. Install all roles except for Active Directory ones. WSUS won't install from Server Manager. Install WSUS 3.0 SP2 (SP1 is what would normally come with Server 2008 so SP2 is good enough).
1. Install Active Directory Domain Services. Run dcpromo.
1. Install the remaining AD roles (create a user in AD for AD RMS to install and use the server's fqdn as the RMS federation server name and cluster server name).
1. Go back to Add Features and install Message Queuing > Message Queuing Services > Routing Support.  The other sub item (Message Queuing > Windows 2000 Client Support)  will not install.
1. Run welm. 

Can't install Hyper-V on x64 while already inside a Hyper-V VM. Server 2008 SP2 has to be installed on physical hardware in order to get those logs.

#### Windows Server 2008 R2

On server add roles.

The install all features script causes too many issues on Server 2008 R2 SP1. Installing Hyper-V causes mouse integration to break. Some features don't get installed (Application Server) either. Install everything manually like how it was done for Server 2008 SP2 to get the most features installed. Manually install WSUS. After dcpromo, add remaining features which results in all features/sub-features getting installed. Remove Remote Server Administration Tools > Role Administration Tools > AD and AD LDS Tools > AD DS Tools > Server for NIS Tools. This is so the Server for NIS role service under the AD DS role can be installed. Add role services to AD CS, AD DS, Print and Document Services, Remote Desktop Services.

Can't install ADFS > FS Proxy, File Services > Windows Server 2003 File Services, Remove Desktop Services > Remote Desktop Services Virtualization Host, and Hyper-V.


Install remaining features that are listed as Disabled at the very end. Run the script. After reboot everything will have to be done by keyboard but that's easy since all that's left is running welm.bat.

#### Windows Server 2012

#### Windows Server 2012 R2

#### Windows Server 2016
