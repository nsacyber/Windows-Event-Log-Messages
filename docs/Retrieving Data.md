# Retrieving data

This section describes how to maximize the amount of event information available for retrieval by WELM. A truly exhaustive and complete amount of event information will never be able to be retrieved for a number of reasons:

1. Some event information can't be loaded due to errors in their definitions.
1. Some features can't be installed simultaneously in Windows preventing retrieval of associated event information.

## Host system preparation

What you need:

1. A compiled debug or release version of WELM (see [Building WELM](./Building%20WELM.md)).
1. A host system running Windows 10 1607 or later (required for [PowerShell Direct](https://docs.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/powershell-direct) [persistent session](https://docs.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/powershell-direct#copy-files-with-new-pssession-and-copy-item) and NAT switch support).
1. Hyper-V installed on the host system.
1. A Hyper-V [NAT switch](https://docs.microsoft.com/en-us/1irtualization/hyper-v-on-windows/user-guide/setup-nat-network) if you want the system to access the Internet to install patches from Windows Update.
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
* [Windows Server 2012 R2 Update](#windows-server-2012-r2-update)
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
1. Click **Switch to Classic View**.
1. Select **Add or Remove Programs**.
1. Click **Add/Remove Windows Components**.
1. Make sure all features have a check mark with a white background next to them rather than check mark with a gray background. If the background is gray, then select the feature and click the **Details** button. Repeat this for all features AND sub-features.
1. Click **Next**.
1. Click **Finish**.
1. Reboot the system.
1. Install .Net 4.0.
1. Open an administrator command prompt.
1. Type **cd C:\\Documents and Settings\\user\\Desktop\\dist\\release** and press **Enter**.
1. Type **welm.bat** and press **Enter**. It should take 10-15 minutes to complete.
1. Open one of the welm.yyyyMMddHHmms\_info.txt log files and copy the WELM ID from the third line of the file. Create a new folder on the Desktop using the WELM ID as the name.
1. Copy the contents of the **welm** folders, that is inside the **\\Desktop\\dist\\release\\** folder, to inside the folder named after the WELM ID.
1. Make sure there are no welm.yyyyMMddHHmms\_fatal.txt log files. If there is, then run welm.bat from the dist\\debug\\ folder and open an issue in the WELM GitHub repository. Add the contents of the file, or attach it, to the issue.
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
1. Open one of the welm.yyyyMMddHHmms\_info.txt log files and copy the WELM ID from the third line of the file. Create a new folder on the Desktop using the WELM ID as the name.
1. Copy the contents of the **wevtutil** and **welm** folders, that are inside the **\\Desktop\\dist\\release\\** folder, to inside the folder named after the WELM ID.
1. Make sure there are no welm.yyyyMMddHHmms\_fatal.txt log files. If there is, then run welm.bat from the dist\\debug\\ folder and open an issue in the WELM GitHub repository and add the contents of the file, or attach it, to the issue.
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
1. Type **cd C:\\users\\user\\Desktop\\dist\\release** and press **Enter**.
1. Type **powershell.exe** and press **Enter**.
1. Type **Set-ExecutionPolicy Unrestricted** and press **Enter**.
1. Type **. .\\Install-Features.ps1** and press **Enter**.
1. Type **$result=Invoke-InstallFeatures -Verbose** and press **Enter**.
1. Once the script has completed, type **$result** and press **Enter**.
1. If **NeedsReboot** is true, then restart the system.
1. If **NeedsInstall** is true, then run the script again after the system reboots. Repeat this until NeedsInstall is false.
1. Reboot the VM if needed and then login to the VM.
1. Open an administrator command prompt.
1. Type **cd C:\\users\\user\\Desktop\\dist\\release** and press **Enter**.
1. Type **welm.bat** and press **Enter**. It should take 10-15 minutes to complete.
1. Open one of the welm.yyyyMMddHHmms\_info.txt log files and copy the WELM ID from the third line of the file. Create a new folder on the Desktop using the WELM ID as the name.
1. Copy the contents of the **wevtutil** and **welm** folders, that are inside the **\\Desktop\\dist\\release\\** folder, to inside the folder named after the WELM ID.
1. Make sure there are no welm.yyyyMMddHHmms\_fatal.txt log files. If there is, then run welm.bat from the dist\\debug\\ folder and open an issue in the WELM GitHub repository and add the contents of the file, or attach it, to the issue.
1. Shutdown the VM.
1. Find the .vhdx file on the host system that represents the VM's hard drive.
1. Right click on the .vhdx file and select **Mount**. The drive root should open in Windows Explorer.
1. Browse the path of **\\users\\user\\Desktop\\** of the VM drive.
1. Copy the WELM ID folder from inside the VM drive to the host system and close the Windows Explorer window for the VM drive.
1. Browse to **This PC** in Windows Explorer on the host system, right click the drive letter (probably D or E) that corresponds to the VM drive and select **Eject**.

#### Windows 8.1

Follow the instructions in the [Windows 8](#windows-8) section.

#### Windows 8.1 Update

Follow the instructions in the [Windows 8](#windows-8) section. You will need to rename the output folder to differentiate it from Windows 8.1 without the update. Insert \_u\_ between "8.1" and "enterprise".

#### Windows 10 1507

Follow the instructions in the [Windows 8](#windows-8) section.

#### Windows 10 1511

Follow the instructions in the [Windows 8](#windows-8) section.

#### Windows 10 1607 and later

Starting with Windows 10 1607, the [Automate-WELM.ps1](..\welm\Automate-WELM.ps1) script is provided to automate retrieval of data generated by WELM. The host must be at least Windows 10 1607 with Hyper-V installed and the guest must be Windows 10 1607 or later since the script uses PowerShell Direct ([1](https://msdn.microsoft.com/en-us/virtualization/hyperv_on_windows/user_guide/vmsession), [2](https://technet.microsoft.com/en-us/windows-server-docs/compute/hyper-v/manage/manage-windows-virtual-machines-with-powershell-direct)).

Create a template virtual machine that can be cloned by the script. The virtual machine should have Internet access OR a Windows installation DVD inserted into its virtual DVD drive. Run the following command on the host as an administrator:

```powershell
powershell.exe -File "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\Automate-WELM.ps1" -VMNetwork 172.16.0 -VMNetworkPrefixSize 24 -VMName "Windows 10 1809 Enterprise x64" -HostStagingPath "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist" -HostResultPath "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\docs\data" -VMStagingPath "C:\transfer\in" -VMResultPath "C:\transfer\out" -HostTranscriptPath "C:\users\user\Desktop" 
```

```powershell
powershell.exe -File "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\Automate-WELM.ps1" -VMNetwork 172.16.0 -VMNetworkPrefixSize 24 -VMName "Windows 10 1809 Enterprise x64" -HostStagingPath "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist" -HostResultPath "C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\docs\data" -VMStagingPath "C:\transfer\in" -VMResultPath "C:\transfer\out" -HostTranscriptPath "C:\users\user\Desktop" -DnsServer '8.8.8.8' -NoWindowsUpdate -Verbose
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

Run welm.bat.

#### Windows Server 2008

Don't install AD CS, AD FS, AD LDS, AD RMS, DNS, DHCP, TS until after installing AD DS and running dcpromo.

On the server it brings up the Roles and Features dialog. Install all the features possible and reboot. There will only be two sub-items that can't install since they need to be installed once domain joined:

* Message Queuing > Message Queuing Services > Routing Service
* Message Queuing > Windows 2000 Client Support

After running dcpromo and rebooting, install the above features.

1. Install .Net 4.0
1. Install all roles except for Active Directory ones. WSUS won't install from Server Manager. Install WSUS 3.0 SP2 (SP1 is what would normally come with Server 2008 so SP2 is good enough).
1. Install Active Directory Domain Services. Run dcpromo.
1. Install the remaining AD roles (create a user in AD for AD RMS to install and use the server's fqdn as the RMS federation server name and cluster server name).
1. Go back to Add Features and install Message Queuing > Message Queuing Services > Routing Support. The other sub item (Message Queuing > Windows 2000 Client Support) will not install.
1. Run welm.bat.

Insert more complicated steps...

RMS issue: <https://social.technet.microsoft.com/wiki/contents/articles/253.the-password-could-not-be-validated-when-attempting-to-provision-an-ad-rms-server.aspx>

#### Windows Server 2008 R2

Follow the instructions in the [Windows 8](#windows-8) section.

1. Install .Net 4.0
1. Run Invoke-InstallFeatures twice.
1. Give an IP address.
1. Uninstall Active Directory Certificate Services
1. Run dcpromo
1. Run Invoke-InstallFeatures again to install AD CS.

The OEMHelpCustomization and CorporateHelpCustomization features fail to install with an error of 1603.

Installing Index Server causes some roles and features to not be able to be installed:

* Roles: Application Server, WSUS
* Features: Windows Internal Database

1. Uninstall Indexing Server by typing **dism.exe /online /disable-feature /featurename:Indexing-Service-Package** and pressing **Enter**.
1. Reboot.
1. Install the WSUS role by connecting the system to the internet and installing it through Add Roles or by using the [offline installer](https://www.microsoft.com/en-us/download/details.aspx?id=5216). WSUS will install the Windows Internal Database feature.
1. Install the Application Server role and all its sub roles/features.
1. Run Invoke-InstallFeatures again to install the Indexing Service.
1. Run welm.bat.

#### Windows Server 2012

Follow the instructions in the [Windows 8](#windows-8) section.

1. Give the system an IP address.
1. In Server Manager go to **Manage** > **Remove Roles and Features**.
1. Remove AD CS.
1. Reboot **shutdown /r /f /t 0**.
1. In Server Manager select AD DS from the left hand pane.
1. Click **More...** link in the yellow bar on the right hand pane.
1. Click **Promote this server to a domain controller** from under the Action column.
1. Run Invoke-InstallFeatures again to install AD CS.
1. Run welm.bat.

#### Windows Server 2012 R2

Follow the instructions in the [Windows Server 2012](#windows-server-2012) section.

#### Windows Server 2012 R2 Update

Follow the instructions in the [Windows Server 2012 R2](#windows-server-2012-r2) section.

You will need to rename the output folder to differentiate it from Windows Server 2012 R2 without the update. Insert \_u\_ between "R2" and "datacenter".

#### Windows Server 2016

Configure the network. Give the system a static IP address. The IP address and Primary DNS address should be the same. Secondary DNS address should be 127.0.0.1. Set a Gateway address.

Follow the instructions in the [Windows 8](#windows-8) section.

After the initial reboot, open an administrator command prompt, type **shutdown /a** and press Enter.

Run Invoke-InstallFeatures again.

The Server-Gui-mgmt\_onecore feature fails to install with an error code of 50.

There is a bug which prevents setting the gateway after installing all features. Without this, the server can't become a domain controller.