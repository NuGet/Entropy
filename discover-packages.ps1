$ErrorActionPreference = "Stop"

$testDir = "$PSScriptRoot\perftests\testCases"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/v5.5.1/nuget.exe"
$nugetPath = "$PSScriptRoot\nuget.exe"

$progressPreference = "silentlyContinue"
try {
    if (!(Test-Path $nugetPath)) {
        Invoke-WebRequest $nugetUrl -OutFile $nugetPath
    }

    $testCases = Get-ChildItem "$testDir\Test-*.ps1"
    $testOutputDir = "$PSScriptRoot\work"
    $nupkgOutputDir = "$PSScriptRoot\nupkgs"

    Remove-Item -Path "$nupkgOutputDir\*.download" | Out-Null
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

    # Determine all of the IDs from the logs.
    Write-Host "Finding the IDs of HTTPs request in logs..."
    $logIds = Get-ChildItem $PSScriptRoot\*-ids.txt `
        | ForEach-Object { Get-Content $_ } `
        | Sort-Object `
        | Get-Unique

    # Download all versions of all package IDs from nuget.org.
    $serviceIndex = Invoke-RestMethod "https://api.nuget.org/v3/index.json"
    $packageBaseAddress = $serviceIndex.resources `
        | Where-Object { ($_ | Select-Object -ExpandProperty "@type") -eq "PackageBaseAddress/3.0.0" } `
        | ForEach-Object { $_ | Select-Object -ExpandProperty "@id" }
    $packageBaseAddress = $packageBaseAddress.TrimEnd("/")

    foreach ($id in $logIds) {
        Write-Host "Downloading all versions of $id..."
        $lowerId = $id.ToLowerInvariant()
        $versionListUrl = "$packageBaseAddress/$lowerId/index.json"
        try {

            $versionList = Invoke-RestMethod $versionListUrl
        }
        catch {
            if ($ex.Exception.Response.StatusCode.value__) {
                continue;
            }
        }
        Write-Host "Found $($versionList.versions.Count) versions."
        foreach ($version in $versionList.versions) {
            $lowerVersion = $version.ToLowerInvariant()
            $fileName = "$lowerId.$lowerVersion.nupkg"
            $nupkgPath = "$PSScriptRoot\nupkgs\$fileName"

            if (Test-Path $nupkgPath) {
                continue;
            }

            Write-Host "Downloading version $version..."
            $nupkgUrl = "$packageBaseAddress/$lowerId/$lowerVersion/$fileName"
            Invoke-WebRequest $nupkgUrl -OutFile "$nupkgPath.download"
            Move-Item "$nupkgPath.download" $nupkgPath
        }
    }
}
finally {
    $progressPreference = "Continue"
}