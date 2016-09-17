#requires -version 4
Set-StrictMode -Version 4

Function Get-ClassicEventLogStatistics() {
    <#
    .SYNOPSIS
    Gets log statistics for classic style Windows logs.

    .DESCRIPTION
    Gets log statistics for classic style Windows logs.

    .PARAMETER Path
    The path to classiclogs.json.

    .EXAMPLE
    Get-ClassicEventLogStatistics -Path 'C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist\classiclogs.json'
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to the JSON file that holds classic log information')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*\.json$')]
        [System.IO.FileInfo]$Path     
    )

    $content = Get-Content $Path -Raw
    $logsJson = ConvertFrom-Json $content

    
    $logStatistics = [pscustomobject]@{
        'Logs' = $logsJson.Count;
    }

    return $logStatistics
}

Function Get-EventLogStatistics() {
    <#
    .SYNOPSIS
    Gets log statistics for modern style Windows logs.

    .DESCRIPTION
    Gets log statistics for modern style Windows logs.

    .PARAMETER Path
    The path to logs.json.

    .EXAMPLE
    Get-EventLogStatistics -Path 'C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist\logs.json'
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to the JSON file that holds log information')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*\.json$')]
        [System.IO.FileInfo]$Path     
    )

    $content = Get-Content $Path -Raw
    $logsJson = ConvertFrom-Json $content

    $logStatistics = @{}

    $logStatistics.Add('Logs', $logsJson.Count)

    $enabledStatistics = $logsJson | Group-Object { $_.IsEnabled } | Sort-Object -Property Name -Descending

    $logStatistics.Add('Enabled', $enabledStatistics[0].Count);
    $logStatistics.Add('Disabled', $enabledStatistics[1].Count);

    $classicStatistics = $logsJson | Group-Object { $_.IsClassic } | Sort-Object -Property Name -Descending

    $logStatistics.Add('Classic', $classicStatistics[0].Count);
    $logStatistics.Add('Modern', $classicStatistics[1].Count);

    $typeStatistics = $logsJson | Group-Object { $_.LogType } | Sort-Object -Property Name # Operational, Analytical, Administrative, Debug

    $typeStatistics | ForEach-Object { $logStatistics.Add($_.Name, $_.Count) }

    $isolationStatistics = $logsJson | Group-Object { $_.Isolation } | Sort-Object -Property Name # Application, System, Custom

    $isolationStatistics | ForEach-Object { $logStatistics.Add($_.Name, $_.Count) }

    $retentionStatistics = $logsJson | Group-Object { $_.Retention } | Sort-Object -Property Name # Circular, Retain, AutoBackup

    $retentionStatistics | ForEach-Object { $logStatistics.Add($_.Name, $_.Count) }

    return [pscustomobject]$logStatistics
}

Function Get-ClassicEventStatistics() {    
<#
    .SYNOPSIS
    Gets event statistics for classic style Windows events.

    .DESCRIPTION
    Gets event statistics for classic style Windows events.

    .PARAMETER Path
    The path to classicevents.json.

    .EXAMPLE
    Get-ClassicEventStatistics -Path 'C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist\classicevents.json'
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to the JSON file that holds classic event information')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*\.json$')]
        [System.IO.FileInfo]$Path     
    )

    $content = Get-Content $Path -Raw
    $eventsJson = ConvertFrom-Json $content

    $eventsCount = $eventsJson.Count    

    $eventsWithMessagesCount = @($eventsJson | Where-Object { $_.Message -ne $null -and $_.Message -ne ''}).Count

    $eventsWithNoMessageCount = @($eventsJson | Where-Object { $_.Message -eq $null -or $_.Message -eq ''}).Count

    $eventsWithMessagesAndParamsCount = @($eventsJson | Where-Object { $_.Message -ne $null -and $_.Message -ne '' -and $_.Message.Contains('%') }).Count

    $eventStatistics = [pscustomobject]@{
        'Events' = $eventsCount;
        'EventsWithMessage' = $eventsWithMessagesCount;
        'EventsWithNoMessage' = $eventsWithNoMessageCount;
        'EventsWithMessageAndParameter' = $eventsWithMessagesAndParamsCount;
    }

    return $eventStatistics
}

Function Get-EventStatistics() {
    <#
    .SYNOPSIS
    Gets event statistics for modern style Windows events.

    .DESCRIPTION
    Gets event statistics for modern style Windows events.

    .PARAMETER Path
    The path to events.json.

    .EXAMPLE
    Get-EventStatistics -Path 'C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist\events.json'
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to the JSON file that holds event information')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*\.json$')]
        [System.IO.FileInfo]$Path     
    )

    $content = Get-Content $Path -Raw
    $eventsJson = ConvertFrom-Json $content

    $eventsCount = $eventsJson.Count    

    $eventsWithMessagesCount = @($eventsJson | Where-Object { $_.Message -ne $null -and $_.Message -ne ''}).Count

    $eventsWithNoMessageCount = @($eventsJson | Where-Object { $_.Message -eq $null -or $_.Message -eq ''}).Count

    $eventsWithMessagesAndParamsCount = @($eventsJson | Where-Object { $_.Message -ne $null -and $_.Message -ne '' -and $_.Message.Contains('%') }).Count

    $eventsWithNoMessageButParamsCount = @($eventsJson | Where-Object { $_.Message -eq $null -or $_.Message -eq '' -and ([bool](($_.Parameters | Get-Member -MemberType NoteProperty) -ne $null )) }).Count

    $eventStatistics = [pscustomobject]@{
        'Events' = $eventsCount;
        'EventsWithMessage' = $eventsWithMessagesCount;
        'EventsWithNoMessage' = $eventsWithNoMessageCount;
        'EventsWithMessageAndParameter' = $eventsWithMessagesAndParamsCount;
        'EventsWithNoMessageButParameter' = $eventsWithNoMessageButParamsCount;
    }

    return $eventStatistics
}

Function Get-ClassicSourceStatistics() {
    <#
    .SYNOPSIS
    Gets source statistics for classic style Windows logs.

    .DESCRIPTION
    Gets source statistics for classic style Windows logs.

    .PARAMETER Path
    The path to classicsources.json.

    .EXAMPLE
    Get-ClassicSourceStatistics -Path 'C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist\classicsources.json'
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to the JSON file that holds classic source information')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*\.json$')]
        [System.IO.FileInfo]$Path     
    )

    $content = Get-Content $Path -Raw
    $sourceJson = ConvertFrom-Json $content

    
    $sourceStatistics = [pscustomobject]@{
        'Sources' = $sourceJson.Count;
    }

    return $sourceStatistics
}


Function Get-ProviderStatistics() {
    <#
    .SYNOPSIS
    Gets provider statistics for modern style Windows logs.

    .DESCRIPTION
    Gets provider statistics for modern style Windows logs.

    .PARAMETER Path
    The path to providers.json.

    .EXAMPLE
    Get-ProviderStatistics -Path 'C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist\providers.json'
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to the JSON file that holds provider information')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*\.json$')]
        [System.IO.FileInfo]$Path     
    )

    $content = Get-Content $Path -Raw
    $providerJson = ConvertFrom-Json $content
   
    $providerStatistics = [pscustomobject]@{
        'Providers' = $providerJson.Count;
    }

    return $providerStatistics
}

Function New-Statistics() {
    <#
    .SYNOPSIS
    Creates statistics files.

    .DESCRIPTION
    Creates statistics files.

    .PARAMETER Path
    The path to the folder containing the .json files.

    .EXAMPLE
    New-Statistics -Path 'C:\Users\user\Documents\GitHub\Windows-Event-Log-Messages\welm\dist'
    #>
    [CmdletBinding()]
    [OutputType([void])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to the JSON file that holds provider information')]
        [ValidateNotNullOrEmpty()]
        [System.IO.DirectoryInfo]$Path     
    )

    $file = 'classiclogs.json'
    $filePath = Join-Path -Path $Path -ChildPath $file
    if (Test-Path -Path $filePath -PathType Leaf) {
        $classicLogStatistics = Get-ClassicEventLogStatistics -Path $filePath
        $classicLogStatistics | ConvertTo-Json | Out-File -FilePath ($filePath.Replace('.json','.stats.json')) -Force
    }

    $file = 'logs.json'
    $filePath = Join-Path -Path $Path -ChildPath $file
    if (Test-Path -Path $filePath -PathType Leaf) {
        $logStatistics = Get-EventLogStatistics -Path $filePath
        $logStatistics | ConvertTo-Json | Out-File -FilePath ($filePath.Replace('.json','.stats.json')) -Force
    }

    $file = 'classicevents.json'
    $filePath = Join-Path -Path $Path -ChildPath $file
    if (Test-Path -Path $filePath -PathType Leaf) {
        $classicEventStatistics = Get-ClassicEventStatistics -Path $filePath
        $classicEventStatistics | ConvertTo-Json | Out-File -FilePath ($filePath.Replace('.json','.stats.json')) -Force
    }

    $file = 'events.json'
    $filePath = Join-Path -Path $Path -ChildPath $file
    if (Test-Path -Path $filePath -PathType Leaf) {
        $eventStatistics = Get-EventStatistics -Path $filePath
        $eventStatistics | ConvertTo-Json | Out-File -FilePath ($filePath.Replace('.json','.stats.json')) -Force
    }

    $file = 'classicsources.json'
    $filePath = Join-Path -Path $Path -ChildPath $file
    if (Test-Path -Path $filePath -PathType Leaf) {
        $classicSourceStatistics = Get-ClassicSourceStatistics -Path $filePath
        $classicSourceStatistics | ConvertTo-Json | Out-File -FilePath ($filePath.Replace('.json','.stats.json')) -Force
    }

    $file = 'providers.json'
    $filePath = Join-Path -Path $Path -ChildPath $file
    if (Test-Path -Path $filePath -PathType Leaf) {
        $providerStatistics = Get-ProviderStatistics -Path $filePath
        $providerStatistics | ConvertTo-Json | Out-File -FilePath ($filePath.Replace('.json','.stats.json')) -Force
    }
}