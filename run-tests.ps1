$ErrorActionPreference = "Stop"

$testDir = "$PSScriptRoot\scripts\perftests\testCases"
$nugetPath = "$PSScriptRoot\nuget.exe"
$testCases = Get-ChildItem "$testDir\Test-*.ps1"

# Download NuGet, if it does not exist yet.
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/v5.5.1/nuget.exe"
if (!(Test-Path $nugetPath)) { Invoke-WebRequest $nugetUrl -OutFile $nugetPath }

# Run all of the test cases, just using the warm-up
foreach ($path in $testCases) {
    & $path `
        -nugetClientFilePath $nugetPath `
        -resultsFilePath $PSScriptRoot\out\results.csv  `
        -logsFolderPath $PSScriptRoot\out\logs `
        -sourceRootFolderPath $PSScriptRoot\out\_s `
        -nugetFoldersPath $PSScriptRoot\out\_t `
        -iterationCount 5 `
        -skipColdRestores `
        -skipForceRestores `
        -skipNoOpRestores
}
