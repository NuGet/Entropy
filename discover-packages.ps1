Param(
    [switch] $skipWarmup
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\scripts\perftests\PerformanceTestUtilities.ps1"

$nupkgDir = Join-Path $PSScriptRoot "out\nupkgs"
$packageHelper = Join-Path $PSScriptRoot "src\PackageHelper\PackageHelper.csproj"
$extraPackages = @(
    "Microsoft.Build.NoTargets" # Needed by NuGet.Client configure.ps1
)

if (!$skipWarmup) {
    & (Join-Path $PSScriptRoot "run-tests.ps1") `
        -resultsName "results-discover-packages" `
        -fast `
        -dumpNupkgsPath $nupkgDir
}

foreach ($extraPackage in $extraPackages) {
    $extraPackageDir = Join-Path $nupkgDir $extraPackage.ToLowerInvariant()
    if (!(Test-Path $extraPackageDir)) {
        New-Item $extraPackageDir -Type Directory | Out-Null
    }
}

# Download all versions of all .nupkgs
Log "Downloading all versions of every discovered ID" "Cyan"
dotnet run download-all-versions --project $packageHelper
Log "Complete." "Cyan"
