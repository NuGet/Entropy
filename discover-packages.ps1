$ErrorActionPreference = "Stop"

$testDir = "$PSScriptRoot\perftests\testCases"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/v5.5.1/nuget.exe"
$nugetPath = "$PSScriptRoot\nuget.exe"

if (!(Test-Path $nugetPath)) {
    Invoke-WebRequest $nugetUrl -OutFile $nugetPath
}

$testCases = Get-ChildItem "$testDir\Test-*.ps1"
$testOutputDir = "$PSScriptRoot\work"
$nupkgOutputDir = "$PSScriptRoot\nupkgs"

foreach ($path in $testCases) {
    $testName = $path.Name.Split("-", 2)[1].Split(".", 2)[0]
    $outDir = "$testOutputDir\$testName"

    & $path `
        -nugetClientFilePath $nugetPath `
        -sourceRootFolderPath $outDir\source `
        -resultsFolderPath $outDir\results  `
        -logsFolderPath $outDir\logs `
        -nugetFoldersPath $outDir\nuget `
        -dumpNupkgsPath $nupkgOutputDir `
        -iterationCount 1 `
        -skipCleanRestores `
        -skipColdRestores `
        -skipForceRestores `
        -skipNoOpRestores
}
