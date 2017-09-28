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
    $OutputDirectory = get-location
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

Write-Host "Running: msbuild /t:restore /bl:$blFilePath /p:Restoreforce=true $InputFile"
msbuild /t:restore /bl:$blFilePath /p:Restoreforce=true $InputFile | out-null

Write-Host "Running: msbuild /t:GenerateRestoreGraphFile /p:RestoreGraphOutputPath=$dgFilePath $InputFile"
msbuild /t:GenerateRestoreGraphFile /p:RestoreGraphOutputPath=$dgFilePath $InputFile | out-null

Write-Host "Running: msbuild /pp:$ppFilePath $InputFile"
msbuild /pp:$ppFilePath $InputFile | out-null

Write-Host "Running: Compress-Archive -Path $OutputDirectory -DestinationPath $zipFilePath"
Compress-Archive -Force -Path $OutputDirectory -DestinationPath $zipFilePath

Write-Host "Clean up"
Remove-Item -ea si $dgFilePath | out-null
Remove-Item -ea si $ppFilePath | out-null
Remove-Item -ea si  $blFilePath | out-null