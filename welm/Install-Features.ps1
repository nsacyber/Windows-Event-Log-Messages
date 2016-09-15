#requires -version 2
Set-StrictMode -Version 2

$version = [system.environment]::osversion.version.major.tostring() + "." + [system.environment]::osversion.version.minor.tostring()
$version = [decimal]$version

if ($version -lt 6.1) {
   Write-Error -Message 'dism does not exist on this OS version"'
   return
}

$mediapath = ''

if($version -ge 6.2) {
   # get CD/DVD drive letter
   $driveletter = Get-WmiObject -Class Win32_LogicalDisk -Filter "DriveType=5" | select DeviceID -expand DeviceID
  
   if ($driveletter -eq $null) {
      Write-Error 'No CD/DVD drive found'
      return
   }

   $size = Get-WmiObject Win32_LogicalDisk | select Size -expand Size

   if($size -eq $null) {
      Write-Error -Message "No disk inserted into CD/DVD drive $driveletter"
      return
   }

   if($size -eq 0) {
      Write-Error -Message "Empty disk in CD/DVD drive $driveletter"
      return
   }

   if(-not (Join-Path $driveletter '\sources\sxs' | Test-Path -pathType container)) {
      Write-Error -Message "$driveletter does not contain Windows installation media"
      return
   }

   $mediapath = Join-Path $driveletter "\sources\sxs"
}


$osType = Get-WmiObject Win32_OperatingSystem | select ProductType -expand ProductType

$features = dism.exe /online /get-features /format:table

$errorcode = $lastexitcode

if ($errorcode -eq 183) {
   Write-Error -Message 'dism is busy so wait or reboot if this keeps happening'
   return
} elseif ($errorcode -ne 0) {
   Write-Error -Message 'dism returned an error code of $errorcode'
   return
}


for ($i=12; $i -lt $features.Count-2; $i++) {

   $indexofbar = $features[$i].indexof("|")

   $featurename = $features[$i].substring(0,$indexofbar).trim()

   $featurestate = $features[$i].substring($indexofbar+2, $features[$i].length-$indexofbar-2).trim()

   if ($featurestate -eq "Enabled") {
      Write-Verbose -Message "$featurename was skipped because it was already installed" 
} elseif (($featurename -eq "Microsoft-Hyper-V" -or $featurename -eq "VmHostAgent") -and $osType -ne 1 -and $version -eq 6.1) {
      # installing Hyper-V on Server 2008 R2 causes the keyboard to stop working
      # install it manually after installing all the other features and roles
      # once that is done then run these commands

      # dism /online /enable-feature /featurename:Microsoft-Hyper-V /NoRestart /Quiet
      # dism /online /enable-feature /featurename:VmHostAgent /NoRestart /Quiet
      # dism /online /enable-feature /featurename:Microsoft-Windows-RemoteFX-Host-Package /NoRestart /Quiet
      # dism /online /enable-feature /featurename:Microsoft-Windows-RemoteFX-EmbeddedVideoCap-Setup-Package /NoRestart /Quiet

      Write-Warning -Message "$featurename was skipped since it breaks keyboard integration. install this feature manually at the very end" 
   } else {
      
	  # check if system is on the internet or not

      if ($featurename -eq "NetFx3") {
         if ($version -ge 6.2) {
            dism /online /enable-feature /featurename:$featurename /All /NoRestart /Quiet /LimitAccess /Source:$mediapath 2>&1 | out-null
         } else {
            dism /online /enable-feature /featurename:$featurename /NoRestart /Quiet /LimitAccess /Source:$mediapath 2>&1 | out-null
         }
      } else {
         if ($version -ge 6.2) {
            dism /online /enable-feature /featurename:$featurename /All /NoRestart /Quiet 2>&1 | out-null
         } else {
            dism /online /enable-feature /featurename:$featurename /NoRestart /Quiet 2>&1 | out-null
         }
      }

      $errorcode = $lastexitcode

      if ($errorcode -eq 0) {
         Write-Verbose -Message "$featurename install succeeded" 
      } elseif ($errorcode -eq 3010) {
         Write-Verbose -Message "$featurename install succeeded and requires a system restart" 
      } elseif ($errorcode -eq 50) {
         Write-Warning -Message "$featurename install failed. a required feature was not already installed" 
      } else {
         Write-Warning -Message "$featurename install failed with exit code $errorcode" 
      }
   }
}

return