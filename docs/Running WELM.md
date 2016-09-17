# Instructions for retrieving event information

Install all features and roles in the operating system before running WELM to get the most complete output. This document describes how to do that for each major Windows operating system release starting with Windows XP.

WELM requires administrative rights to retrieve all event information. Specifically it needs administrative rights to get event information about the Security log, its related providers, and its events.

## How to Retrieve Data 
Generally, here is what is required to retrieve data with WELM.
1. Create a virtual machine.
1. Install WELM prerequisites (.Net 4.0) in the virtual machine.
1. Enable all features in Windows either manually (XP, 2003, Vista, Server 2008) or with the provided Install-Features.ps1 PowerShell script (Windows 7/Server 2008 R2 and later). Run dcpromo on the server editions of the operating system in order to allow retrieving of all possible events.
1. Copy welm.exe, NLog.config, and welm.bat to the virtual machine.
1. Run welm.bat.
1. Copy the generated data out of the virtual machine.
1. Run **New-Statistics -Path 'C:\path to folder containing generated data'** from the Get-Statistics.ps1 file to generate statistics files based on the data.


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


### Windows Vista SP2/Server 2008 SP2

1. While it is possible to script setup with pkgmgr and/or ocsetup, they are not ideal. It is much faster to select all the features manually in the UI. Make sure all items have a check mark rather than a filled in square.

1. Start > Settings > Control Panel > Programs > Turn Windows Features on or off. On the server it brings up the Roles and Features dialog. Install all the features possible and reboot. There will only be two sub-items that can't install: Message Queuing > Message Queuing Services > Routing Support and Message Queuing > Windows 2000 Client Support.
1. Install .Net 3.5.
1. Install all roles except for Active Directory ones. WSUS won't install from Server Manager. Install WSUS 3.0 SP2 (SP1 is what would normally come with Server 2008 so SP2 is good enough).
1. Install Active Directory Domain Services. Run dcpromo.
1. Install the remaining AD roles (create a user in AD for AD RMS to install and use the server's fqdn as the RMS federation server name and cluster server name).
1. Go back to Add Features and install Message Queuing > Message Queuing Services > Routing Support.  The other sub item (Message Queuing > Windows 2000 Client Support)  will not install.
1. Run welm.


Can't install Hyper-V on x64 while already inside a Hyper-V VM. Server 2008 SP2 has to be installed on physical hardware in order to get those logs.


### Windows 7 SP1/Server 2008 R2 SP1

Net 3.5 is already installed by default on Windows 7. 

1. Set-ExecutionPolicy Unrestricted -Force
1. Run the install all features script and reboot.

On server add roles.

The install all features script causes too many issues on Server 2008 R2 SP1. Installing Hyper-V causes mouse integration to break. Some features don't get installed (Application Server) either. Install everything manually like how it was done for Server 2008 SP2 to get the most features installed. Manually install WSUS. After dcpromo, add remaining features which results in all features/sub-features getting installed. Remove Remote Server Administration Tools > Role Administration Tools > AD and AD LDS Tools > AD DS Tools > Server for NIS Tools. This is so the Server for NIS role service under the AD DS role can be installed. Add role services to AD CS, AD DS, Print and Document Services, Remote Desktop Services.

Can't install ADFS > FS Proxy, File Services > Windows Server 2003 File Services, Remove Desktop Services > Remote Desktop Services Virtualization Host, and Hyper-V.


Install remaining features that are listed as Disabled at the very end. Run the script. After reboot everything will have to be done by keyboard but that's easy since all that's left is running welm.bat.


### Windows 8/Server 2012

To install .Net 3.5 on an internet disconnected Windows 8 system, insert the Windows 8 ISO into the virtual drive and then run this command: dism /online /Enable-Feature /FeatureName:NetFx3 /All /LimitAccess /Source:D:\sources\sxs The /Source path will vary based on which drive letter was assigned to the drive that contains the Windows image.

Run the install all features script and reboot.

Check all features are installed. If not, run the script again and reboot.


### Windows 8.1/Server 2012 R2

Same as Windows 8/Server 2012.


Give it a network card in the Hyper-V settings.
In the VM change it to a static IP.
Remove Active Directory Certificate Services's Certification Authority sub option only. It needs to be removed because dcpromo doesn't want it. Do this from the Turn Windows Features on or off
Go to Server Manager > AD DS > click the More... link in the light yellow bar thingy. Click the Promote this server to a domain controller link under the Action column. Follow the steps to create a new domain.
Install the Certification Authority sub option again.


To collect all the events that are possible, run WELM on physical machines for any edition of Windows that supports Hyper-V. For servers, this is obvious. For non-servers, this applies to Windows 8 and later since Enterprise Edition supports client Hyper-V. Can't install Hyper-V into a virtual machine (at least not in a Hyper-V VM). The script appears to install Hyper-V even when it is in a VM.