<#
.SYNOPSIS
Builds and Runs NuGetBuildValidators.
#>
[CmdletBinding()]
param (
    [Alias('v14')]
    [string]$VS14VSIXPath,
    [Alias('v15')]
    [string]$VS15VSIXPath,
    [Alias('v15Ins')]
    [string]$VS15InsVSIXPath,
    [Parameter(Mandatory=$True)]
    [Alias('uz')]
    [string]$VSIXUnzipPath,
    [Parameter(Mandatory=$True)]
    [Alias('l')]
    [string]$LogPath,
    [Parameter(Mandatory=$True)]
    [Alias('tfs')]
    [string]$NuGetTFSPath,
    [Alias('f')]
    [switch]$Fast
)

. ".\common.ps1"

Write-Host ("`r`n" * 3)
Trace-Log ('=' * 60)

$MSBuildExe = msbuild
$startTime = [DateTime]::UtcNow
$BuildErrors = @()

Trace-Log "Validating #$BuildNumber started at $startTime"

Invoke-BuildStep 'Building NuGetValidators.Sln' {
        
        # Build NuGetBuildValidators.Sln
        Trace-Log ". `"$MSBuildExe`" NuGetValidators.Sln"
        & $MSBuildExe NuGetValidators.Sln

        if (-not $?)
        {
            Write-Error "Building NuGetValidators.Sln failed!"
            exit 1
        }
    } `
    -skip:$Fast `
    -ev +BuildErrors
    
Invoke-BuildStep 'Run NuGetBuildValidators.Localization for VS14' {
        
        $NuGetBuildValidatorsLocalizationExe = ".\NuGetValidators.Localization\bin\Debug\NuGetValidator.Localization.exe"
        $NuGetTFSCommentsPath = Join-Path $NuGetTFSPath 14
        $VSIXLogPath = Join-Path $LogPath "14"
        
        # Run NuGetBuildValidators.Localization
        Trace-Log ". `"$NuGetBuildValidatorsLocalizationExe`" $VS14VSIXPath $VSIXUnzipPath $VSIXLogPath $NuGetTFSPath"
        & $NuGetBuildValidatorsLocalizationExe $VS14VSIXPath $VSIXUnzipPath $VSIXLogPath $NuGetTFSPath

        if (-not $?)
        {
            Write-Error "Run NuGetBuildValidators.Localization failed"
        }
    } `
    -Skip:(-not $VS14VSIXPath) `
    -ev +BuildErrors
    
Invoke-BuildStep 'Run NuGetBuildValidators.Localization for VS15' {
        
        $NuGetBuildValidatorsLocalizationExe = ".\NuGetValidators.Localization\bin\Debug\NuGetValidator.Localization.exe"
        $NuGetTFSCommentsPath = Join-Path $NuGetTFSPath 15
        $VSIXLogPath = Join-Path $LogPath "15"
        
        # Run NuGetBuildValidators.Localization
        Trace-Log ". `"$NuGetBuildValidatorsLocalizationExe`" $VS15VSIXPath $VSIXUnzipPath $VSIXLogPath $NuGetTFSPath"
        & $NuGetBuildValidatorsLocalizationExe $VS15VSIXPath $VSIXUnzipPath $VSIXLogPath $NuGetTFSPath

        if (-not $?)
        {
            Write-Error "Run NuGetBuildValidators.Localization failed"
        }
    } `
    -Skip:(-not $VS15VSIXPath) `
    -ev +BuildErrors
    
Invoke-BuildStep 'Run NuGetBuildValidators.Localization for VS15Insertable' {
        
        $NuGetBuildValidatorsLocalizationExe = ".\NuGetValidators.Localization\bin\Debug\NuGetValidator.Localization.exe"
        $NuGetTFSCommentsPath = Join-Path $NuGetTFSPath 15
        $VSIXLogPath = Join-Path $LogPath "15Ins"
        
        # Run NuGetBuildValidators.Localization
        Trace-Log ". `"$NuGetBuildValidatorsLocalizationExe`" $VS15InsVSIXPath $VSIXUnzipPath $VSIXLogPath $NuGetTFSPath"
        & $NuGetBuildValidatorsLocalizationExe $VS15InsVSIXPath $VSIXUnzipPath $VSIXLogPath $NuGetTFSPath

        if (-not $?)
        {
            Write-Error "Run NuGetBuildValidators.Localization failed"
        }
    } `
    -Skip:(-not $VS15InsVSIXPath) `
    -ev +BuildErrors


Trace-Log ('-' * 60)

## Calculating Build time
$endTime = [DateTime]::UtcNow
Trace-Log "Build #$BuildNumber ended at $endTime"
Trace-Log "Time elapsed $(Format-ElapsedTime ($endTime - $startTime))"

Trace-Log ('=' * 60)

if ($BuildErrors) {
    $ErrorLines = $BuildErrors | %{ ">>> $($_.Exception.Message)" }
    Write-Error "Build's completed with $($BuildErrors.Count) error(s):`r`n$($ErrorLines -join "`r`n")" -ErrorAction Stop
}

Write-Host ("`r`n" * 3)
