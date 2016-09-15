#requires -version 4
Set-StrictMode -Version 4

Function Get-ClassicEventLogStatistics() {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to the JSON file that holds modern event information')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*\.json$')]
        [System.IO.FileInfo]$Path     
    )

}

Function Get-EventLogStatistics() {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to the JSON file that holds modern event information')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*\.json$')]
        [System.IO.FileInfo]$Path     
    )

}

Function Get-ClassicEventStatistics() {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to the JSON file that holds modern event information')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*\.json$')]
        [System.IO.FileInfo]$Path     
    )

}

Function Get-EventStatistics() {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to the JSON file that holds modern event information')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*\.json$')]
        [System.IO.FileInfo]$Path     
    )

    $content = Get-Content $Path
    $eventsJson = ConvertFrom-Json $content

    $eventsCount = $eventsJson.Count    

    $eventsWithMessagesCount =  @($eventsJson | Where-Object { $_.Message -ne $null -and $_.Message -ne ''}).Count

    $eventsWithNoMessageCount =  @($eventsJson | Where-Object { $_.Message -eq $null -or $_.Message -eq ''}).Count

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
