#requires -version 4
Set-StrictMode -Version 4

Function Get-ExplicitFacilities() {
    <#
    .SYNOPSIS
    Gets explicitly defined NT Facility code names and values.

    .DESCRIPTION
    Gets explicitly defined NT Facility code names and values.

    .PARAMETER Path
    The path to ntstatus.h.

    .EXAMPLE
    Get-ExplicitFacilities -Path 'C:\Program Files (x86)\Windows Kits\10\Include\10.0.14393.0\shared\ntstatus.h' 

    .EXAMPLE
    (Get-ExplicitFacilities -Path 'C:\Program Files (x86)\Windows Kits\10\Include\10.0.14393.0\shared\ntstatus.h').GetEnumerator() | Sort-Object -Property Name -Descending

    .EXAMPLE
    (Get-ExplicitFacilities -Path 'C:\Program Files (x86)\Windows Kits\10\Include\10.0.14393.0\shared\ntstatus.h').Count

    .EXAMPLE
    (Get-ExplicitFacilities -Path 'C:\Program Files (x86)\Windows Kits\10\Include\10.0.14393.0\shared\ntstatus.h').GetEnumerator() | Sort-Object -Property Value 
    #>
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to ntstatus.h')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*ntstatus\.h$')]
        [System.IO.FileInfo]$Path     
    )

    $content = Get-Content $Path -Raw

    $lines = $content -split [System.Environment]::NewLine
    
    $facilities = @{}

    $lines | ForEach-Object {
        if ($_ -match "^.*#define\s+(FACILITY|FACILTIY)_(\w*)\s+0x([0-9a-fA-F]+)\s*$") {
            $name = $matches[2]
            $value = [int]('0x{0}' -f $matches[3])

            if ($facilities.ContainsKey($name)) {
                Write-Warning -Message ("Line: '{0}' contains a duplicate facility name of '{1}' with value 0x{2:X}" -f $_,$name,$value)
            } else {
                $facilities.Add($name, $value)
            }
        }

    }

    return $facilities
}

Function Get-NtStatusCodes() {
    <#
    .SYNOPSIS
    Gets explicitly defined NTSTATUS code names and values.

    .DESCRIPTION
    Gets explicitly defined NTSTATUS code names and values.

    .PARAMETER Path
    The path to ntstatus.h.

    .EXAMPLE
    Get-NtStatusCodes -Path 'C:\Program Files (x86)\Windows Kits\10\Include\10.0.14393.0\shared\ntstatus.h' 

    .EXAMPLE
    (Get-NtStatusCodes -Path 'C:\Program Files (x86)\Windows Kits\10\Include\10.0.14393.0\shared\ntstatus.h').Count

    .EXAMPLE
    (Get-NtStatusCodes -Path 'C:\Program Files (x86)\Windows Kits\10\Include\10.0.14393.0\shared\ntstatus.h').GetEnumerator() | Sort-Object -Property Value | ForEach-Object { ('{0} 0x{1:X}') -f $_.Name,$_.Value }
    #>
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to ntstatus.h')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*ntstatus\.h$')]
        [System.IO.FileInfo]$Path     
    )

    $content = Get-Content $Path -Raw

    $lines = $content -split [System.Environment]::NewLine
    
    $codes = @{}

    $count = (@($lines | Where-Object { $_.Contains('#define STATUS_') })).Count

    $lines | ForEach-Object {
        #if ($_ -match "^.*#define\s+STATUS_(\w*)\s+\(\(NTSTATUS\)0x([0-9a-fA-F]+)L\)\s*$") {
        if ($_ -match "^.*#define\s+STATUS_(\w*)\s+.*0x([0-9a-fA-F]+).*$") {
            $name = $matches[1]
            $value = [long]('0x{0}' -f $matches[2])

            if ($codes.ContainsKey($name)) {
                Write-Warning -Message ("Line: '{0}' contains a duplicate NTSTATUS name of '{1}' with value 0x{2:X}" -f $_,$name,$value)
            } else {
                $codes.Add($name, $value)
            }
        }
    }

    if ($count -ne $codes.Count){
        Write-Warning -Message ('Count of NTSTATUS codes did not match due to possible parsing errors. Simple: {0} Regex: {1}' -f $count,$codes.Count)
    }

    return $codes
}


Function Get-Bits() {
    <#
    .SYNOPSIS
    Get specific bits from a value.

    .DESCRIPTION
    Get specific bits from a value. It is 0-based so on a 32-bit value use 0 through 31.

    .PARAMETER MostSignificantBit
    The most significant bit number to start at, inclusive.

    .PARAMETER LeastSignificantBit
    The least significant bit number to start at, inclusive.

    .PARAMETER Number
    The number to take the bits from.
    #>
    [CmdletBinding()]
    [OutputType([long])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The most significant bit number to start at, inclusive.')]
        [ValidateNotNullOrEmpty()]
        [ValidateRange(0,31)]
        [int]$MostSignificantBit,

        [Parameter(Position=1, Mandatory=$true, HelpMessage='The least significant bit number to start at, inclusive.')]
        [ValidateNotNullOrEmpty()]
        [ValidateRange(0,31)]
        [int]$LeastSignificantBit,
        
        [Parameter(Position=2, Mandatory=$true, HelpMessage='The number to take the bits from.')]
        [ValidateNotNullOrEmpty()]
        [long]$Number                      
    )
    
    if ($MostSignificantBit -le $LeastSignificantBit) {
        throw 'Most significant bit must be greater than least significant bit'
    }

    return ($Number -shr $LeastSignificantBit) -band -bnot(-bnot(0) -shl ($MostSignificantBit - $LeastSignificantBit + 1))
}

Function Get-ImplicitFacilities() {
    <#
    .SYNOPSIS
    Gets implicitly defined NT Facility code names and values.

    .DESCRIPTION
    Gets implicitly defined NT Facility code names and values.

    .PARAMETER Path
    The path to ntstatus.h.

    .EXAMPLE
    Get-ImplicitFacilities -Path 'C:\Program Files (x86)\Windows Kits\10\Include\10.0.14393.0\shared\ntstatus.h' 

    .EXAMPLE
    (Get-ImplicitFacilities -Path 'C:\Program Files (x86)\Windows Kits\10\Include\10.0.14393.0\shared\ntstatus.h').GetEnumerator() | Sort-Object -Property Name -Descending

    .EXAMPLE
    (Get-ImplicitFacilities -Path 'C:\Program Files (x86)\Windows Kits\10\Include\10.0.14393.0\shared\ntstatus.h').Count

    .EXAMPLE
    (Get-ImplicitFacilities -Path 'C:\Program Files (x86)\Windows Kits\10\Include\10.0.14393.0\shared\ntstatus.h').GetEnumerator() | Sort-Object -Property Value 
    #>
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    Param(
        [Parameter(Position=0, Mandatory=$true, HelpMessage='The path to ntstatus.h')]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^.*ntstatus\.h$')]
        [System.IO.FileInfo]$Path     
    )

    $facilities = @{}

    $codes = Get-NtStatusCodes -Path $Path

    $codes.GetEnumerator() | ForEach-Object {
        $name = $_.Name
        $value = $_.Value

        $facility = Get-Bits -MostSignificantBit 27 -LeastSignificantBit 16 -Number $value

        if($facilities.ContainsKey($facility)) {
            #$v = $facilities[$facility]
            #$v++
            #$facilities[$facility] = $v

            $n = $facilities[$facility]
            $n += $name
            $facilities[$facility]= $n
        } else {
            #$facilities.Add($facility, 1)

            $facilities.Add($facility, [string[]]@($name))
        }
    }

    return $facilities
}