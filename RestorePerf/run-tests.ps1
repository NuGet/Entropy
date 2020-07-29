Param(
    [string] $variantName,
    [switch] $fast,
    [int] $iterations = 10,
    [string[]] $sources,
    [string] $dumpNupkgsPath,
    [string] $resultsName,
    [string[]] $testCases,
    [string] $nugetPath,
    [string] $sdkVersion
)

. "$PSScriptRoot\scripts\perftests\PerformanceTestUtilities.ps1"

if (!$testCases) {
    $testDir = Join-Path $PSScriptRoot "scripts\perftests\testCases"
    $testCases = Get-ChildItem (Join-Path $testDir "Test-*.ps1")
}

if (!$nugetPath) {
    $nugetPath = Get-NuGetExePath
}

if ($sdkVersion) {
    $globalJson = Join-Path $PSScriptRoot "out\_s\global.json"
    Log "Writing global.json to $globalJson with SDK version $sdkVersion."
    WriteGlobalJson $globalJson $sdkVersion
}

ValidateVariantName $variantName

if ($iterations -lt 1) { $fast = $true }
if (!$resultsName -and $variantName) { $resultsName = $variantName }
if ($fast -and !$resultsName) { $resultsName = "fast" }
if (!$fast -and !$resultsName) { throw 'The -variantName or -resultsName parameter is required when not using -fast.' }

foreach ($testCasePath in $testCases) {
    if ($variantName) {
        Log "Starting variant $variantName, test case $testCasePath" "Cyan"
    } else {
        Log "Starting test case $testCasePath" "Cyan"
    }
    
    & $testCasePath `
        -nugetClientFilePath $nugetPath `
        -resultsFilePath (Join-Path $PSScriptRoot "out\results-$resultsName.csv") `
        -logsFolderPath (Join-Path $PSScriptRoot "out\logs") `
        -sourceRootFolderPath (Join-Path $PSScriptRoot "out\_s") `
        -nugetFoldersPath (Join-Path $PSScriptRoot "out\_t") `
        -dumpNupkgsPath $dumpNupkgsPath `
        -iterationCount $iterations `
        -skipCleanRestores:$fast `
        -skipColdRestores `
        -skipForceRestores `
        -skipNoOpRestores `
        -variantName $variantName `
        -sources $sources

    if ($variantName) {
        Log "Finished variant $variantName, test case $testCasePath" "Cyan"
    } else {
        Log "Finished test case: $testCasePath" "Cyan"
    }
}
