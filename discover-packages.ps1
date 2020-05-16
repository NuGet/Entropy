Param(
    [switch] $skipWarmup
)

$ErrorActionPreference = "Stop"

$testDir = Join-Path $PSScriptRoot "scripts\perftests\testCases"
$nupkgDir = Join-Path $PSScriptRoot "out\nupkgs"
$nugetPath = Join-Path $PSScriptRoot "nuget.exe"
$packageHelper = Join-Path $PSScriptRoot "src\PackageHelper\PackageHelper.csproj"
$testCases = Get-ChildItem (Join-Path $testDir "Test-*.ps1")
$extraPackages = @("Microsoft.Build.NoTargets")

# Download NuGet, if it does not exist yet.
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/v5.5.1/nuget.exe"
if (!(Test-Path $nugetPath)) { Invoke-WebRequest $nugetUrl -OutFile $nugetPath }

# Run all of the test cases, just using the warm-up
if (!$skipWarmup) {
    foreach ($path in $testCases) {
        & $path `
            -nugetClientFilePath $nugetPath `
            -resultsFilePath (Join-Path $PSScriptRoot "out\results-discover-packages.csv")  `
            -logsFolderPath (Join-Path $PSScriptRoot "out\logs") `
            -dumpNupkgsPath $nupkgDir `
            -sourceRootFolderPath (Join-Path $PSScriptRoot "out\_s") `
            -nugetFoldersPath (Join-Path $PSScriptRoot "out\_t ")`
            -iterationCount 1 `
            -skipCleanRestores `
            -skipColdRestores `
            -skipForceRestores `
            -skipNoOpRestores
    }
}

foreach ($extraPackage in $extraPackages) {
    $extraPackageDir = Join-Path $nupkgDir $extraPackage.ToLowerInvariant()
    if (!(Test-Path $extraPackageDir)) {
        New-Item $extraPackageDir -Type Directory | Out-Null
    }
}

# Download all versions of all .nupkgs
dotnet run download-all-versions --project $packageHelper
