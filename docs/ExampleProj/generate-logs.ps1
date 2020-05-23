Param(
    [int] $iterationCount = 20
)

. (Join-Path $PSScriptRoot "..\..\scripts\perftests\PerformanceTestUtilities.ps1")

# Download NuGet, if it does not exist yet.
$nugetPath = Join-Path $PSScriptRoot "nuget.exe"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/v5.5.1/nuget.exe"
if (!(Test-Path $nugetPath)) { Invoke-WebRequest $nugetUrl -OutFile $nugetPath }

$objDir = (Join-Path $PSScriptRoot "obj")
$env:NUGET_PACKAGES=(Join-Path $objDir "upf")
$env:NUGET_HTTP_CACHE_PATH=(Join-Path $objDir "hc")

for ($i = 1; $i -le $iterationCount; $i++) {
    if (Test-Path $objDir) {
        Remove-Item $objDir -Recurse -Force;
    }

    $logDate = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssffff");
    $logPath = "$PSScriptRoot\restoreLog-nuget-ExampleProj-$logDate.txt"
    Log "[$i/$iterationCount] Logging to $logPath"
    
    & "$PSScriptRoot\nuget.exe" `
        restore "$PSScriptRoot\ExampleProj.csproj" `
        > $logPath
}
