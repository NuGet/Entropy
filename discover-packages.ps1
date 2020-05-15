$ErrorActionPreference = "Stop"

$testDir = "$PSScriptRoot\perftests\testCases"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/v5.5.1/nuget.exe"
$nugetPath = "$PSScriptRoot\nuget.exe"

if (!(Test-Path $nugetPath)) {
    Invoke-WebRequest $nugetUrl -OutFile $nugetPath
}

$testCases = Get-ChildItem "$testDir\Test-*.ps1"

foreach ($path in $testCases) {
    $testName = $path.Name.Split("-", 2)[1].Split(".", 2)[0]

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
