Param(
    [int] $iterations = 20
)

. (Join-Path $PSScriptRoot "..\..\scripts\perftests\PerformanceTestUtilities.ps1")

$nugetPath = Get-NuGetExePath

$objDir = (Join-Path $PSScriptRoot "obj")
$env:NUGET_PACKAGES=(Join-Path $objDir "upf")
$env:NUGET_HTTP_CACHE_PATH=(Join-Path $objDir "hc")

for ($i = 1; $i -le $iterations; $i++) {
    if (Test-Path $objDir) {
        Remove-Item $objDir -Recurse -Force;
    }

    $logDate = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssffff");
    $logPath = "$PSScriptRoot\restoreLog-nuget-ExampleProj-$logDate.txt"
    Log "[$i/$iterations] Logging to $logPath"
    
    & "$PSScriptRoot\nuget.exe" `
        restore "$PSScriptRoot\ExampleProj.csproj" `
        > $logPath
}
