Param(
    [Parameter(Mandatory = $True)]
    [string] $nugetClientFilePath,
    [Parameter(Mandatory = $True)]
    [string] $sourceRootFolderPath,
    [Parameter(Mandatory = $True)]
    [string] $resultsFilePath,
    [Parameter(Mandatory = $True)]
    [string] $logsFolderPath,
    [string] $nugetFoldersPath,
    [string] $dumpNupkgsPath,
    [int] $iterationCount,
    [switch] $skipWarmup,
    [switch] $skipCleanRestores,
    [switch] $skipColdRestores,
    [switch] $skipForceRestores,
    [switch] $skipNoOpRestores,
    [string] $variantName,
    [string[]] $sources
)

$scriptsDir = Join-Path $PSScriptRoot "..\scripts\perftests"

. (Join-Path $scriptsDir "PerformanceTestUtilities.ps1")

$sourceFolderPath = Join-Path $PSScriptRoot "..\docs\ExampleProj"
$configFiles = @("NuGet.Config")
$solutionFilePath = Join-Path $sourceFolderPath "ExampleProj.csproj"

SetPackageSources $nugetClientFilePath $sourceFolderPath $configFiles $sources
SetupNuGetFolders $nugetClientFilePath $nugetFoldersPath

. (Join-Path $scriptsDir "RunPerformanceTests.ps1") `
    -nugetClientFilePath $nugetClientFilePath `
    -solutionFilePath $solutionFilePath `
    -resultsFilePath $resultsFilePath `
    -logsFolderPath $logsFolderPath `
    -nugetFoldersPath $nugetFoldersPath `
    -dumpNupkgsPath $dumpNupkgsPath `
    -iterationCount $iterationCount `
    -skipWarmup:$skipWarmup `
    -skipCleanRestores:$skipCleanRestores `
    -skipColdRestores:$skipColdRestores `
    -skipForceRestores:$skipForceRestores `
    -skipNoOpRestores:$skipNoOpRestores `
    -variantName $variantName
