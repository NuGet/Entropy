Param(
    [int] $iterations = 20,
    [string] $variantName,
    [string] $solutionName,
    [switch] $noConfirm
)

. "$PSScriptRoot\..\scripts\perftests\PerformanceTestUtilities.ps1"

if ($variantName -and !$solutionName) {
    throw "The -solutionName parameter is required when using the -variantName parameter."
}

ValidateVariantName $variantName

$logsDir = Join-Path $PSScriptRoot "..\out\logs"
if (Test-Path $logsDir) {
    Remove-Item $logsDir -Force -Recurse -Confirm:(!$noConfirm)
}

$graphsDir = Join-Path $PSScriptRoot "..\out\graphs"
if (Test-Path $graphsDir) {
    Remove-Item $graphsDir -Force -Recurse -Confirm:(!$noConfirm)
}

if ($solutionName) {
    if ($variantName) {
        $restoreLogPattern = "restoreLog-$variantName-$solutionName-*.txt"
    } else {
        $restoreLogPattern = "restoreLog-$solutionName-*.txt"
    }
} else {
    $restoreLogPattern = "restoreLog-*-*.txt"
}

$restoreLogPattern = Join-Path $PSScriptRoot "..\out\all-logs\$restoreLogPattern"
$allLogs = Get-ChildItem $restoreLogPattern `
    | Sort-Object -Property Name

if (!$allLogs) {
    throw "No restore logs were found with pattern: $restoreLogPattern"
}

Log "$($allLogs.Count) restore logs were found:"
foreach ($log in $allLogs) {
    Log "- $($log.FullName)"
}

for ($logCount = 1; $logCount -le $allLogs.Count; $logCount++) {
    Log "Starting the test with $logCount log(s), $iterations iteration(s)" "Cyan"

    if (Test-Path $logsDir) {
        Remove-Item $logsDir -Force -Recurse
    }
    
    New-Item $logsDir -Type Directory | Out-Null

    $allLogs `
        | Select-Object -First $logCount `
        | ForEach-Object { Copy-Item $_ (Join-Path $logsDir $_.Name) }
    
    Log "Parsing the restore log(s)." "Green"
    dotnet run `
        --configuration Release `
        --framework netcoreapp3.1 `
        --project (Join-Path $PSScriptRoot "..\src\PackageHelper\PackageHelper.csproj") `
        -- `
        parse-restore-logs
    
    if ($solutionName) {
        if ($variantName) {
            $requestGraphPath = Join-Path $graphsDir "requestGraph-$variantName-$solutionName.json.gz"
        } else {
            $requestGraphPath = Join-Path $graphsDir "requestGraph-$solutionName.json.gz"
        }
    } else {
        $requestGraphPath = Get-ChildItem (Join-Path $graphsDir "requestGraph-*.json.gz") `
            | Sort-Object -Property Name `
            | Select-Object -First 1 -Property FullName
    }

    Log "Replaying the request graph." "Green"
    dotnet run `
        --configuration Release `
        --framework netcoreapp3.1 `
        --project (Join-Path $PSScriptRoot "..\src\PackageHelper\PackageHelper.csproj") `
        -- `
        replay-request-graph `
        $requestGraphPath `
        --iterations $iterations

    Log "Finished the test with $logCount log(s), $iterations iteration(s)" "Cyan"
}
