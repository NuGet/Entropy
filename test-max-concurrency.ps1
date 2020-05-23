Param(
    [Parameter(Mandatory = $true)]
    [string[]] $variantNames,
    [int] $iterations = 20
)

. "$PSScriptRoot\scripts\perftests\PerformanceTestUtilities.ps1"

$requestGraphs = @()

foreach ($variantName in $variantNames) {
    $pathPattern = Join-Path $PSScriptRoot "out/request-graphs/requestGraph-$variantName-*.json.gz"
    $requestGraphs += Get-ChildItem $pathPattern
}

Log "Found $($requestGraphs.Count) request graphs:"
foreach ($requestGraph in $requestGraphs) {
    Log "- $requestGraph"
}

$packageHelper = Join-Path $PSScriptRoot "src\PackageHelper\PackageHelper.csproj"

$maxConcurrency = 64
while ($maxConcurrency -ge 1) {
    Log "Using $iterations iterations and max concurrency of $maxConcurrency" "Green"

    foreach ($requestGraph in $requestGraphs) {
        Log "Starting $requestGraph" "Cyan"

        dotnet run `
            --configuration Release `
            --project $packageHelper `
            -- `
            replay-request-graph $requestGraph `
            --iterations $iterations `
            --max-concurrency $maxConcurrency

        Log "Finished $requestGraph" "Cyan"
    }

    $maxConcurrency /= 2
}
