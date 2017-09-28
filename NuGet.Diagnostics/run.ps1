<#
.SYNOPSIS
Generates diagnostic information for NuGet project or solution.

.PARAMETER InputFile
A project or solution file to be used to generate the diagnostic data.

.PARAMETER OutputDirectory
A directory to be used to store the diagnostic data.

.EXAMPLE
run.ps1 -InputFile .\a.csproj -OutputDirectory .\logs
#>

[CmdletBinding()]
param (
    [Alias('i')]
    [string]$InputFile,
    [Alias('o')]
    [string]$OutputDirectory
)

$dgFileName = "out.dg"
$ppFileName = "out.pp"
$blFileName = "out.binlog"
$zipFileName = "out.zip"

if ([string]::IsNullOrEmpty($OutputDirectory))
{
    $OutputDirectory = "./";
}
else
{
    if (![System.IO.Path]::IsPathRooted($OutputDirectory))
    {
        $OutputDirectory = [System.IO.Path]::Combine((get-location), $OutputDirectory)
    }
    if (![System.IO.Directory]::Exists($OutputDirectory))
    {
        New-Item -ItemType directory -Path $OutputDirectory
    }
    
}

$dgFilePath = [System.IO.Path]::Combine($OutputDirectory, $dgFileName)
$ppFilePath = [System.IO.Path]::Combine($OutputDirectory, $ppFileName)
$blFilePath = [System.IO.Path]::Combine($OutputDirectory, $blFileName)
$zipFilePath = [System.IO.Path]::Combine($OutputDirectory, $zipFileName)

Write-Host "Running: msbuild /t:GenerateRestoreGraphFile /p:RestoreGraphOutputPath=$dgFilePath"
msbuild /t:GenerateRestoreGraphFile /p:RestoreGraphOutputPath=$dgFilePath

Write-Host "Running: msbuild /pp:$ppFilePath"
msbuild /pp:$ppFilePath

Write-Host "Running: msbuild /t:restore /bl:$blFilePath"
msbuild /t:restore /bl:$blFilePath

Write-Host "Running: Compress-Archive -Path $OutputDirectory -DestinationPath $zipFilePath"
Compress-Archive -Path $OutputDirectory -DestinationPath $zipFilePath