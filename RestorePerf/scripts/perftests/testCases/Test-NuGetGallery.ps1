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

. "$PSScriptRoot\..\PerformanceTestUtilities.ps1"

$repoUrl = "https://github.com/NuGet/NuGetGallery.git"
$testCaseName = GenerateNameFromGitUrl $repoUrl

RunPerformanceTestsOnGitRepository `
    -nugetClientFilePath $nugetClientFilePath `
    -sourceRootFolderPath $sourceRootFolderPath `
    -testCaseName $testCaseName `
    -repoUrl $repoUrl `
    -commitHash "765917957b830837af97818e24a7a7be78f440ec" `
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
    -configFiles @("NuGet.config", "tests\NuGet.Config") `
    -variantName $variantName `
    -sources $sources
