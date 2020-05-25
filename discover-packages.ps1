Param(
    [switch] $skipWarmup,
    [string[]] $testCases,
    [int] $maxDownloadsPerId = [int]::MaxValue
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
        -variantName "discoverpackages" `
        -fast `
        -dumpNupkgsPath $nupkgDir `
        -testCases $testCases
}

foreach ($extraPackage in $extraPackages) {
    $extraPackageDir = Join-Path $nupkgDir $extraPackage.ToLowerInvariant()
    if (!(Test-Path $extraPackageDir)) {
        New-Item $extraPackageDir -Type Directory | Out-Null
    }
}

# Download all versions of all .nupkgs
$maxDownloadsPerIdStr = if ($maxDownloadsPerId -lt [int]::MaxValue) { $maxDownloadsPerId } else { "all" }
Log "Downloading $maxDownloadsPerIdStr versions of every discovered ID" "Cyan"
dotnet run `
    --configuration Release `
    --framework netcoreapp3.1 `
    --project $packageHelper `
    -- `
    download-all-versions `
    --max-downloads-per-id $maxDownloadsPerId
Log "Complete." "Cyan"
