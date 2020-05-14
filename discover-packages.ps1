$testDir = "$PSScriptRoot\perftests\testCases"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/v5.5.1/nuget.exe"
$nugetPath = "$PSScriptRoot\nuget.exe"

if (!(Test-Path $nugetPath)) {
    $progressPreference = 'silentlyContinue'
    try {
        Invoke-WebRequest $nugetUrl -OutFile $nugetPath
    } finally {
        $progressPreference = 'Continue'
    }
}

$testCases = Get-ChildItem "$testDir\Test-*.ps1"
$testOutputDir = "$PSScriptRoot\work"
$nupkgOutputDir = "$PSScriptRoot\nupkgs"

Remove-Item -Path $nupkgOutputDir -Force -Recurse | Out-Null
Remove-Item -Path "$PSScriptRoot\*-ids.txt" | Out-Null

foreach ($path in $testCases) {
    $testName = $path.Name.Split("-", 2)[1].Split(".", 2)[0]
    $outDir = "$testOutputDir\$testName"

    Remove-Item -Path $outDir\logs\*.txt | Out-Null

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

    Get-ChildItem $outDir\logs\*.txt `
        | ForEach-Object { Get-Content $_ } `
        | Where-Object { $_.StartsWith("  GET https://api.nuget.org/v3-flatcontainer/") } `
        | ForEach-Object { if ($_ -match "v3-flatcontainer/([^/]+)") { $Matches[1] } } `
        | Sort-Object `
        | Get-Unique `
        > $PSScriptRoot\$testName-ids.txt
}

# Determine all of the IDs from the .nupkg dump.
$nupkgIds = & $nugetPath list -source $nupkgOutputDir -Prerelease `
    | ForEach-Object { $_.Split(' ')[0].ToLowerInvariant() } `
    | Sort-Object

# Determine all of the IDs from the logs.
$logIds = Get-ChildItem $PSScriptRoot\*-ids.txt `
    | ForEach-Object { Get-Content $_ } `
    | Sort-Object `
    | Get-Unique

$inNupkgsButNotLogs = $nupkgIds | Where-Object { $_ -notin $logIds }
$inLogsButNotNupkgs = $logsIds | Where-Object { $_ -notin $nupkgIds }
if ($inNupkgsButNotLogs -or $inLogsButNotNupkgs) {
    Write-Error "In .nupkg dump but not logs: $inNupkgsButNotLogs"
    Write-Error "In logs but not .nupkg dump: $inLogsButNotNupkgs"
}
