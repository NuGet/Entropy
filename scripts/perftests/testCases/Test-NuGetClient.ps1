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

$repoUrl = "https://github.com/NuGet/NuGet.Client.git"
$commitHash = "788bc01a1b063a37841cdd6d035feb320e90e475"
$repoName = GenerateNameFromGitUrl $repoUrl
$sourcePath = $([System.IO.Path]::Combine($sourceRootFolderPath, $repoName))
$solutionFilePath = SetupGitRepository $repoUrl $commitHash $sourcePath

# Reset the NuGet config files
SetPackageSources $nugetClientFilePath $sourcePath @("NuGet.Config") $sources

# It's fine if this is run from here. It is run again the performance test script, but it'll set it to the same values.
# Additionally, this will cleanup the extras from the bootstrapping which are already in the local folder, allowing us to get more accurate measurements
SetupNuGetFolders $nugetClientFilePath $nugetFoldersPath
$currentWorkingDirectory = $pwd

Try
{
    Set-Location $sourcePath
    Log "Running NuGet.Client configure.ps1"
    . "$sourcePath\configure.ps1" *>>$null
}
Finally
{
    Set-Location $currentWorkingDirectory
}

. "$PSScriptRoot\..\RunPerformanceTests.ps1" `
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
    -skipNoOpRestores:$skipNoOpRestores
    -variantName $variantName
