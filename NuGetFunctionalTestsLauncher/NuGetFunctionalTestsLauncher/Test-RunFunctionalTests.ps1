param (
	[string]$Branch="dev",
    [Parameter(Mandatory=$true)]
    [string]$CIRoot,
    [Parameter(Mandatory=$true)]
    [string]$Command,
    [Parameter(Mandatory=$true)]
    [string]$NuGetCIToolsFolder,
    [Parameter(Mandatory=$true)]
    [string]$RootDir,
    [int]$TimeoutInSecs=60000,
	[ValidateSet("14.0", "12.0", "11.0", "10.0")][string]$VSVersion="14.0")

function ExtractZip($source, $destination)
{
    Write-Host 'Extracting files from ' $source ' to ' $destination

    $shell = New-Object -ComObject Shell.Application
    $zip = $shell.NameSpace($source)
    $files = $zip.Items()
    # 0x14 means that the existing files will be overwritten silently
    $shell.NameSpace($destination).CopyHere($files, 0x14)
    Write-Host 'Extraction Complete'
}

function ParseResultsHtml($resultsHtmlFile)
{
    $resultsHtmlString = [string]::Join('', (Get-Content $resultsHtmlFile))
    $result = [regex]::matches($resultsHtmlString, 'Ran.*Skipped').Value

    Write-Host 'Parsed Result is ' $result
    if ($result.Contains(", 0 Failed"))
    {
        return @($true, $result)
    }
    return @($false, $result)
}

$env:NuGetDropPath=$CIRoot + '\' + $Branch + '\packages'

$env:NuGetFunctionalTests_TestPath=$RootDir+'\EndToEnd'
$env:NuGetFunctionalTests_VSVersion=$VSVersion
$env:NuGetFunctionalTests_Command=$Command

Write-Host 'NuGetFunctionalTests_TestPath is ' $env:NuGetFunctionalTests_TestPath
Write-Host 'NuGetFunctionalTests_VSVersion is ' $env:NuGetFunctionalTests_VSVersion
Write-Host 'NuGetFunctionalTests_Command is ' $env:NuGetFunctionalTests_Command

Write-Host 'Kill any running instances of devenv...'
(Get-Process 'devenv' -ErrorAction SilentlyContinue) | Kill -ErrorAction SilentlyContinue

if (Test-Path $env:NuGetFunctionalTests_TestPath)
{
    Write-Host 'Deleting ' $env:NuGetFunctionalTests_TestPath ' test path before running tests...'
    rmdir -Recurse $env:NuGetFunctionalTests_TestPath -Force

    if (Test-Path $env:NuGetFunctionalTests_TestPath)
    {
        Write-Error 'Could not delete folder ' $env:NuGetFunctionalTests_TestPath
        exit 1
    }

    Write-Host 'Done.'
}

if (Test-Path $env:temp)
{
    Write-Host 'Deleting temp folder'
    rmdir $env:temp -Recurse -ErrorAction SilentlyContinue
    Write-Host 'Done.'
}

$endToEndZipSrc = $env:NuGetDropPath + '\EndToEnd.zip'

$endToEndZip = $RootDir + '\EndToEnd.zip'

# Delete the zip file if it exists
if (Test-Path $endToEndZip)
{
    Remove-Item $endToEndZip
}

Copy-Item $endToEndZipSrc $endToEndZip -Force

Write-Host 'Creating ' $env:NuGetFunctionalTests_TestPath
mkdir $env:NuGetFunctionalTests_TestPath

Write-Host 'NuGetFunctionalTests_TestPath is ' $env:NuGetFunctionalTests_TestPath

ExtractZip $endToEndZip $env:NuGetFunctionalTests_TestPath

Copy-Item $NuGetCIToolsFolder\*.exe $env:NuGetFunctionalTests_TestPath
Copy-Item $NuGetCIToolsFolder\*.exe.config $env:NuGetFunctionalTests_TestPath
Copy-Item $NuGetCIToolsFolder\*.dll $env:NuGetFunctionalTests_TestPath

# Set the registry key to prevent the 'Not Responding' window from showing up on a crash
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting" -Name ForceQueue -Value 1
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\Consent" -Name DefaultConsent -Value 1

Write-Host 'Before starting the functional tests, force delete all the Results.html under the tests folder'
(Get-ChildItem $env:NuGetFunctionalTests_TestPath -Recurse Results.html) | Remove-Item -Force

Write-Host 'Starting functional tests with a timeout of ' $TimeoutInSecs ' seconds.'

$p = Start-Process $env:NuGetFunctionalTests_TestPath\NuGetFunctionalTestsLauncher.exe -wait -NoNewWindow -PassThru
if ($p.ExitCode -ne 0)
{
    $errorMessage = $env:NuGetFunctionalTests_TestPath + '\NuGetFunctionalTestsLauncher.exe failed to run'
    Write-Error $errorMessage
    exit $p.ExitCode
}

$sleepCounter = 0
$totalSleepCycles = [Math]::Ceiling($TimeoutInSecs / 10)
$sleepCycleDuration = [Math]::Min($TimeoutInSecs, 10)

Write-Host 'Started waiting now. Total timeout : ' $TimeoutInSecs 'secs.'
Write-Host 'Number of sleep cycles: ' $totalSleepCycles '. Duration of each sleep cycle: ' $sleepCycleDuration 'secs.'

While ($sleepCounter -lt $totalSleepCycles)
{
    # On each cycle, wait for 10 seconds
    # and, then check if the Results.html has been created.
    Start-Sleep -Seconds $sleepCycleDuration
    $resultsHtmlFiles = (Get-ChildItem $env:NuGetFunctionalTests_TestPath -Recurse Results.html)
    if ($resultsHtmlFiles.Count -eq 1)
    {
        Write-Host 'Found the results html file. Functional tests have completed run.'
        $result = ParseResultsHtml $resultsHtmlFiles[0]
        if ($result[0] -eq $true)
        {
            Write-Host -ForegroundColor Green 'Run passed. Result: ' $result[1]
            exit 0
        }

        $errorMessage = 'RUN FAILED. Result: ' + $result[1]
        Write-Error $errorMessage
        exit 1
    }

    $sleepCounter++
}

$errorMessage = 'Run Failed - Results.html did not get created in timeout ' + $TimeoutInSecs + ' secs' +
                '. This indicates that the tests did not finish running. It could be that the VS crashed. Please investigate."'

Write-Error $errorMessage
exit 1