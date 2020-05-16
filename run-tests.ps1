Param(
    [string] $resultsName,
    [switch] $fast,
    [int] $iterationCount = 10,
    [string[]] $sources
)

$testDir = Join-Path $PSScriptRoot "scripts\perftests\testCases"
$nugetPath = Join-Path $PSScriptRoot "nuget.exe"
$testCases = Get-ChildItem (Join-Path $testDir "Test-*.ps1")

# Download NuGet, if it does not exist yet.
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/v5.5.1/nuget.exe"
if (!(Test-Path $nugetPath)) { Invoke-WebRequest $nugetUrl -OutFile $nugetPath }

if ($fast -and !$resultsName) { $resultsName = "results-fast"; }
if (!$fast -and !$resultsName) { throw 'The -resultsName parameter is required when not using -fast.' }

foreach ($testCasePath in $testCases) {
    & $testCasePath `
        -nugetClientFilePath $nugetPath `
        -resultsFilePath (Join-Path $PSScriptRoot "out\$resultsName.csv") `
        -logsFolderPath (Join-Path $PSScriptRoot "out\logs") `
        -sourceRootFolderPath (Join-Path $PSScriptRoot "out\_s") `
        -nugetFoldersPath (Join-Path $PSScriptRoot "out\_t") `
        -iterationCount $iterationCount `
        -skipCleanRestores:$fast `
        -skipColdRestores `
        -skipForceRestores `
        -skipNoOpRestores `
        -sources $sources
}
