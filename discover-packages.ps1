$ErrorActionPreference = "Stop"

$testDir = "$PSScriptRoot\scripts\perftests\testCases"
$nugetPath = "$PSScriptRoot\nuget.exe"
$packageHelper = "$PSScriptRoot\src\PackageHelper\PackageHelper.csproj"
$testCases = Get-ChildItem "$testDir\Test-*.ps1"

# Download NuGet, if it does not exist yet.
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/v5.5.1/nuget.exe"
if (!(Test-Path $nugetPath)) { Invoke-WebRequest $nugetUrl -OutFile $nugetPath }

# Run all of the test cases, just using the warm-up
foreach ($path in $testCases) {
    $testName = $path.Name.Split("-", 2)[1].Split(".", 2)[0]

    break

    & $path `
        -nugetClientFilePath $nugetPath `
        -resultsFilePath $PSScriptRoot\out\results.csv  `
        -logsFolderPath $PSScriptRoot\out\logs `
        -dumpNupkgsPath $PSScriptRoot\out\nupkgs `
        -sourceRootFolderPath $PSScriptRoot\out\_s `
        -nugetFoldersPath $PSScriptRoot\out\_t `
        -iterationCount 1 `
        -skipCleanRestores `
        -skipColdRestores `
        -skipForceRestores `
        -skipNoOpRestores
}

# Download all versions of all .nupkgs
dotnet run download-packages --project $packageHelper
