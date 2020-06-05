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
    # Needed by NuGet.Client configure.ps1
    "ILMerge",
    "Microsoft.Build.NoTargets",
    "NuGet.Core",
    "XunitXml.TestLogger",
    @("https://dotnet.myget.org/F/nuget-volatile/api/v3/index.json", "VSLangProj150", "1.0.0"),
    @("https://dotnet.myget.org/F/nuget-volatile/api/v3/index.json", "NuGetValidator", "2.0.2"),
    @("https://dotnet.myget.org/F/nuget-volatile/api/v3/index.json", "NuGet.Client.EndToEnd.TestData", "1.0.0"),
    @("https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json", "Microsoft.DotNet.Maestro.Tasks", "1.1.0-beta.20065.7"),
    @("https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json", "Microsoft.DotNet.Build.Tasks.Feed", "5.0.0-beta.20159.1"),
    @("https://pkgs.dev.azure.com/azure-public/vside/_packaging/vs-impl/nuget/v3/index.json", "Microsoft.VisualStudio.ProjectSystem", "16.0.201-pre-g7d366164d0")
)

if (!$skipWarmup) {
    & (Join-Path $PSScriptRoot "run-tests.ps1") `
        -variantName "discoverpackages" `
        -fast `
        -dumpNupkgsPath $nupkgDir `
        -testCases $testCases
}

foreach ($extraPackage in $extraPackages) {
    if ($extraPackage -is [string]) {
        $extraPackageDir = Join-Path $nupkgDir $extraPackage.ToLowerInvariant()
        if (!(Test-Path $extraPackageDir)) {
            New-Item $extraPackageDir -Type Directory | Out-Null
        }
    } else {
        dotnet run `
            --configuration Release `
            --framework netcoreapp3.1 `
            --project $packageHelper `
            -- `
            download-package $extraPackage[1] $extraPackage[2] `
            --source $extraPackage[0]
        if ($LASTEXITCODE) { throw "Command failed." }
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
if ($LASTEXITCODE) { throw "Command failed." }
Log "Complete." "Cyan"
