<#
.SYNOPSIS
Copy results to the share
#>
[CmdletBinding()]
param (
    [string]$Source,
    [string]$Destination
)

. ".\common.ps1"

Trace-Log "Copying logs from $Source to $Destination"
Robocopy.exe /mt:32 /s /it /is $Source $Destination
if ($? > 1)
{
    Write-Error "Copying the results failed!"
    exit 1
}