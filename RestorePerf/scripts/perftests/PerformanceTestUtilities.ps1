# Contains all the utility methods used by the performance tests.

function Get-NuGetExePath()
{
    $outDir = Join-Path $PSScriptRoot "..\..\out"
    $path = Join-Path $outDir "nuget.exe"
    $url = "https://dist.nuget.org/win-x86-commandline/v5.6.0/nuget.exe"
    
    if (!(Test-Path $outDir))
    {
        New-Item $outDir -Type Directory | Out-Null
    }

    if (!(Test-Path $path))
    {
        $ProgressPreference = "SilentlyContinue"
        try
        {
            Log "Downloading nuget.exe..."
            Invoke-WebRequest $url -OutFile $path
        }
        finally
        {
            $ProgressPreference = "Continue"
        }
    }

    return $path
}

function ValidateVariantName($variantName)
{
    if ($variantName -and $variantName.Contains("-"))
    {
        throw "Variant name '$variantName' must not contain hyphens."
    }
}

function SetPackageSources($nugetClientFilePath, $sourcePath, $configFiles, $sources, [switch]$ignoreChanges)
{
    $configFilePaths = $configFiles | ForEach-Object { Join-Path $sourcePath $_ }

    # Reset all NuGet config files.
    foreach ($configFile in $configFilePaths)
    {
        git -C $sourcePath checkout $configFile --quiet
        if ($LASTEXITCODE) { throw "Command 'git -C $sourcePath checkout $configFile' failed." }
    }

    # Verify that the repository is clean.
    if (!$ignoreChanges) {
        $changes = git -C $sourcePath status --porcelain=v1    
        if ($LASTEXITCODE) { throw "Command 'git -C $sourcePath status --porcelain=v1' failed." }
        if ($changes)
        {
            throw "The source path $sourcePath has changes:`r`n$changes"
        }
    }
    
    if ($sources)
    {
        $nameToSource = [ordered]@{}
        $sources | ForEach-Object { $nameToSource[[Guid]::NewGuid().ToString()] = $_ }

        foreach ($configFile in $configFilePaths)
        {
            # Find all enabled sources.
            if (IsClientDotnetExe $nugetClientFilePath) {
                $allSources = & $nugetClientFilePath nuget list source --configfile $configFile
                if ($LASTEXITCODE) { throw "Command '$nugetClientFilePath nuget list source --configfile $configFil' failed." }
            } else {
                $allSources = & $nugetClientFilePath sources list -ConfigFile $configFile
                if ($LASTEXITCODE) { throw "Command '$nugetClientFilePath sources list -ConfigFile $configFile' failed." }
            }

            $enabledSources = $allSources | ForEach-Object { if ($_ -match "^\s+\d+\.\s+(.+?) \[Enabled\]$") { $Matches[1] } }

            # Disable all enabled sources.
            Log "Disabling default sources in $configFile"
            foreach ($enabledSource in $enabledSources)
            {
                if (IsClientDotnetExe $nugetClientFilePath) {
                    & $nugetClientFilePath nuget disable source $enabledSource --configfile $configFile | Out-Null
                    if ($LASTEXITCODE) { throw "Command '$nugetClientFilePath nuget disable source $enabledSource --configfile $configFile' failed." }
                } else {
                    & $nugetClientFilePath sources disable -Name $enabledSource -ConfigFile $configFile | Out-Null
                    if ($LASTEXITCODE) { throw "Command '$nugetClientFilePath sources disable -Name $enabledSource -ConfigFile $configFile' failed." }
                }
            }
        
            # Add the provided sources.
            foreach ($pair in $nameToSource.GetEnumerator())
            {
                Log "Enabling source '$($pair.Value)' in $configFile"
                
                if (IsClientDotnetExe $nugetClientFilePath) {
                    & $nugetClientFilePath nuget add source $pair.Value --name $pair.Key --configfile $configFile | Out-Null
                    if ($LASTEXITCODE) { throw "Command '$nugetClientFilePath nuget add source $pair.Value --name $pair.Key --configfile $configFile' failed." }
                } else {
                    & $nugetClientFilePath sources add -Name $pair.Key -Source $pair.Value -ConfigFile $configFile | Out-Null
                    if ($LASTEXITCODE) { throw "Command '$nugetClientFilePath sources add -Name $($pair.Key) -Source $($pair.Value) -ConfigFile $configFile' failed." }
                }
            }
        }
    }
}

# The format of the URL is assumed to be https://github.com/NuGet/NuGet.Client.git. The result would be NuGet.Client
function GenerateNameFromGitUrl([string]$gitUrl)
{
    $output = $gitUrl
    if ($output.EndsWith(".git")) { $output = $output.Substring(0, $output.Length - 4) }
    $output = $output.Substring($($gitUrl.LastIndexOf('/') + 1))
    return $output
}

# Appends the log time in front of the log statement with the color specified. 
function Log([string]$logStatement, [string]$color)
{
    $timestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss') + 'Z'
    if([string]::IsNullOrEmpty($color))
    {
        Write-Host "$($timestamp): $logStatement"
    }
    else
    { 
        Write-Host "$($timestamp): $logStatement" -ForegroundColor $color
    }
}

# Given a relative path, gets the absolute path from the current directory
function GetAbsolutePath([string]$Path)
{
    $Path = [System.IO.Path]::Combine((Get-Location).Path, $Path);
    $Path = [System.IO.Path]::GetFullPath($Path);
    return $Path;
}

# Writes the content to the given path. Creates the folder structure if needed
function OutFileWithCreateFolders([string]$path, [string]$content)
{
    $folder = [System.IO.Path]::GetDirectoryName($path)
    If(!(Test-Path $folder))
    {
        & New-Item -ItemType Directory -Force -Path $folder > $null
    }
    Add-Content -Path $path -Value $content
}

# Gets a list of all the files recursively in the given folder
Function GetFiles(
    [Parameter(Mandatory = $True)]
    [string] $folderPath,
    [string] $pattern)
{
    If (Test-Path $folderPath)
    {
        $files = Get-ChildItem -Path $folderPath -Filter $pattern -Recurse -File

        Return $files
    }

    Return $Null
}

# Gets a list of all the nupkgs recursively in the given folder
Function GetPackageFiles(
    [Parameter(Mandatory = $True)]
    [string] $folderPath)
{
    Return GetFiles $folderPath "*.nupkg"
}

Function GetFilesInfo([System.IO.FileInfo[]] $files)
{
    If ($Null -eq $files)
    {
        $count = 0
        $totalSizeInMB = 0
    }
    Else
    {
        $count = $files.Count
        $totalSizeInMB = ($files | Measure-Object -Property Length -Sum).Sum / 1000000
    }

    Return @{
        Count = $count
        TotalSizeInMB = $totalSizeInMB
    }
}

# Determines if the client is dotnet.exe by checking the path.
function GetClientName([string]$nugetClient)
{
    return [System.IO.Path]::GetFileName($nugetClient)
}

function IsClientDotnetExe([string]$nugetClient)
{
    return $nugetClient.EndsWith("dotnet.exe")
}

# Downloads the repository at the given path.
Function DownloadRepository([string] $repository, [string] $commitHash, [string] $sourceFolderPath)
{
    If (Test-Path $sourceFolderPath)
    {
        Log "Skipping the cloning of $repository as $sourceFolderPath is not empty" -color "Yellow"
    }
    Else
    {
        git clone $repository $sourceFolderPath
        git -C $sourceFolderPath checkout $commitHash
    }
}

# Write a global.json file with the specified .NET Core SDK version
Function WriteGlobalJson($path, $version) {
    @{
        "sdk" = @{
            "version" = $version
        }
    } | ConvertTo-Json | Out-File $path -Encoding UTF8
}

# Find the appropriate solution file for the repository. Looks for a solution file matching the repo name, 
# if not it takes the first available sln file in the repo. 
Function GetSolutionFilePath([string] $repository, [string] $sourceFolderPath)
{
    $gitRepoName = $repository.Substring($($repository.LastIndexOf('/') + 1))
    $potentialSolutionFilePath = [System.IO.Path]::Combine($sourceFolderPath, "$($gitRepoName.Substring(0, $gitRepoName.Length - 4)).sln")

    If (Test-Path $potentialSolutionFilePath)
    {
        $solutionFilePath = $potentialSolutionFilePath
    }
    Else
    {
        $possibleSln = Get-ChildItem $sourceFolderPath *.sln
        If ($possibleSln.Length -eq 0)
        {
            Log "No solution files found in $sourceFolderPath" "red"
        }
        Else
        {
            $solutionFilePath = $possibleSln[0] | Select-Object -f 1 | Select-Object -ExpandProperty FullName
        }
    }

    Return $solutionFilePath
}

# Given a repository and a hash, checks out the revision in the given source directory. The return is a solution file if found. 
Function SetupGitRepository([string] $repository, [string] $commitHash, [string] $sourceFolderPath)
{
    Log "Setting up $repository into $sourceFolderPath"
    DownloadRepository $repository $commitHash $sourceFolderPath
    $solutionFilePath = GetSolutionFilePath $repository $sourceFolderPath
    Log "Completed the repository setup. The solution file is $solutionFilePath" -color "Green"

    Return $solutionFilePath
}

# runs locals clear all with the given client
Function LocalsClearAll([string] $nugetClientFilePath)
{
    $nugetClientFilePath = GetAbsolutePath $nugetClientFilePath
    If ($(IsClientDotnetExe $nugetClientFilePath))
    {
        . $nugetClientFilePath nuget locals -c all *>>$null
    }
    Else
    {
        . $nugetClientFilePath locals -clear all -Verbosity quiet
    }
}

# Gets the client version
Function GetClientVersion([string] $nugetClientFilePath)
{
    $nugetClientFilePath = GetAbsolutePath $nugetClientFilePath

    If (IsClientDotnetExe $nugetClientFilePath)
    {
        $version = . $nugetClientFilePath --version
    }
    Else
    {
        $output = . $nugetClientFilePath
        $version = $(($output -split '\n')[0]).Substring("NuGet Version: ".Length)
    }

    Return $version
}

# Gets the default test folder
Function GetDefaultNuGetTestFolder()
{
    Return $Env:UserProfile
}

# Gets the NuGet folders path where all of the discardable data from the tests will be put.
Function GetNuGetFoldersPath([string] $testFoldersPath)
{
    $nugetFoldersPath = [System.IO.Path]::Combine($testFoldersPath, "np")
    return GetAbsolutePath $nugetFoldersPath
}

# Sets up the global packages folder, http cache and plugin caches and cleans them before starting.
Function SetupNuGetFolders([string] $nugetClientFilePath, [string] $nugetFoldersPath)
{
    $Env:NUGET_PACKAGES = [System.IO.Path]::Combine($nugetFoldersPath, "gpf")
    $Env:NUGET_HTTP_CACHE_PATH = [System.IO.Path]::Combine($nugetFoldersPath, "hcp")
    $Env:NUGET_PLUGINS_CACHE_PATH = [System.IO.Path]::Combine($nugetFoldersPath, "pcp")

    # This environment variable is not recognized by any NuGet client.
    $Env:NUGET_SOLUTION_PACKAGES_FOLDER_PATH = [System.IO.Path]::Combine($nugetFoldersPath, "sp")

    LocalsClearAll $nugetClientFilePath
}

# Cleanup the nuget folders and delete the nuget folders path.
# This should only be invoked by the the performance tests
Function CleanNuGetFolders([string] $nugetClientFilePath, [string] $nugetFoldersPath)
{
    Log "Cleanup up the NuGet folders - global packages folder, http/plugins caches. Client: $nugetClientFilePath. Folders: $nugetFoldersPath"

    LocalsClearAll $nugetClientFilePath

    Remove-Item $nugetFoldersPath -Recurse -Force -ErrorAction Ignore

    [Environment]::SetEnvironmentVariable("NUGET_PACKAGES", $Null)
    [Environment]::SetEnvironmentVariable("NUGET_HTTP_CACHE_PATH", $Null)
    [Environment]::SetEnvironmentVariable("NUGET_PLUGINS_CACHE_PATH", $Null)
    [Environment]::SetEnvironmentVariable("NUGET_SOLUTION_PACKAGES_FOLDER_PATH", $Null)
    [Environment]::SetEnvironmentVariable("NUGET_FOLDERS_PATH", $Null)
}

# Given a repository, a client and directories for the results/logs, runs the configured performance tests.
Function RunPerformanceTestsOnGitRepository(
    [string] $nugetClientFilePath,
    [string] $sourceRootFolderPath,
    [string] $testCaseName,
    [string] $repoUrl,
    [string] $commitHash,
    [string] $resultsFilePath,
    [string] $nugetFoldersPath,
    [string] $logsFolderPath,
    [string] $dumpNupkgsPath,
    [int] $iterationCount,
    [switch] $skipWarmup,
    [switch] $skipCleanRestores,
    [switch] $skipColdRestores,
    [switch] $skipForceRestores,
    [switch] $skipNoOpRestores,
    [string[]] $configFiles,
    [string[]] $sources,
    [string] $variantName)
{
    $sourceFolderPath = $([System.IO.Path]::Combine($sourceRootFolderPath, $testCaseName))
    $solutionFilePath = SetupGitRepository -repository $repoUrl -commitHash $commitHash -sourceFolderPath $sourceFolderPath

    SetPackageSources $nugetClientFilePath $sourceFolderPath $configFiles $sources

    SetupNuGetFolders $nugetClientFilePath $nugetFoldersPath
    . "$PSScriptRoot\RunPerformanceTests.ps1" `
        -nugetClientFilePath $nugetClientFilePath `
        -solutionFilePath $solutionFilePath `
        -resultsFilePath $resultsFilePath `
        -logsFolderPath $logsFolderPath `
        -nugetFoldersPath $nugetFoldersPath `
        -dumpNupkgsPath $dumpNupkgsPath `
        -iterationCount $iterationCount `
        -skipWarmup:$skipWarmup `
        -skipCleanRestores:$skipCleanRestores `
        -skipColdRestores:$skipColdRestores `
        -skipForceRestores:$skipForceRestores `
        -skipNoOpRestores:$skipNoOpRestores `
        -variantName $variantName
}

Function GetProcessorInfo()
{
    $processorInfo = Get-WmiObject Win32_processor

    Return @{
        Name = $processorInfo | Select-Object -ExpandProperty Name
        NumberOfCores = $processorInfo | Select-Object -ExpandProperty NumberOfCores
        NumberOfLogicalProcessors = $processorInfo | Select-Object -ExpandProperty NumberOfLogicalProcessors
    }
}

Function LogDotNetSdkInfo($nugetClientFilePath)
{
    Try
    {
        $currentVersion = & $nugetClientFilePath --version
        $currentSdk = & $nugetClientFilePath --list-sdks | Where-Object { $_.StartsWith("$currentVersion ") } | Select-Object -First 1

        Log "Using .NET Core SDK $currentSdk."
    }
    Catch [System.Management.Automation.CommandNotFoundException]
    {
        Log ".NET Core SDK not found." -Color "Yellow"
    }
}

# Note:  System.TimeSpan rounds to the nearest millisecond.
Function ParseElapsedTime(
    [Parameter(Mandatory = $True)]
    [decimal] $value,
    [Parameter(Mandatory = $True)]
    [string] $unit)
{
    Switch ($unit)
    {
        "ms" { Return [System.TimeSpan]::FromMilliseconds($value) }
        "sec" { Return [System.TimeSpan]::FromSeconds($value) }
        "min" { Return [System.TimeSpan]::FromMinutes($value) }
        Default { throw "Unsupported unit of time:  $unit" }
    }
}

Function ExtractProjectRestoreStatistics(
    [Parameter(Mandatory = $True)]
    $lines)
{
    # All packages listed in packages.config are already installed.
    $prefix = "Restore completed in "

    $lines = $lines | Where-Object { $_.IndexOf($prefix) -gt -1 }

    $elapsedTimes = @();
    ForEach ($line In $lines)
    {
        $index = $line.IndexOf($prefix)

        $parts = $line.Substring($index + $prefix.Length).Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)

        $value = [System.Double]::Parse($parts[0])
        $unit = $parts[1]

        $elapsedTimes += (ParseElapsedTime $value $unit).Ticks
    }

    $measurement = $elapsedTimes | Measure-Object -Maximum -Sum -Average
    
    return [ordered]@{
        Count = $measurement.Count;
        Maximum = [TimeSpan]::FromTicks($measurement.Maximum);
        Sum = [TimeSpan]::FromTicks($measurement.Sum);
        Average = [TimeSpan]::FromTicks($measurement.Average);
    }
}

# Plugins cache is only available in 4.8+. We need to be careful when using that switch for older clients because it may blow up.
# The logs location is optional
Function RunRestore(
    [string] $solutionFilePath,
    [string] $nugetClientFilePath,
    [string] $resultsFile,
    [string] $logsFolderPath,
    [string] $dumpNupkgsPath,
    [string] $scenarioName,
    [string] $solutionName,
    [string] $timestampUtc,
    [int] $iteration,
    [int] $iterationCount,
    [string] $variantName,
    [switch] $isPackagesConfig,
    [switch] $cleanGlobalPackagesFolder,
    [switch] $cleanHttpCache,
    [switch] $cleanPluginsCache,
    [switch] $killMsBuildAndDotnetExeProcesses,
    [switch] $force)
{
    $isClientDotnetExe = IsClientDotnetExe $nugetClientFilePath

    If ($isClientDotnetExe -And $isPackagesConfig)
    {
        Log "dotnet.exe does not support packages.config restore." "Red"

        Return
    }

    Log "[$iteration/$iterationCount] Running $nugetClientFilePath restore with cleanGlobalPackagesFolder:$cleanGlobalPackagesFolder cleanHttpCache:$cleanHttpCache cleanPluginsCache:$cleanPluginsCache killMsBuildAndDotnetExeProcesses:$killMsBuildAndDotnetExeProcesses force:$force"

    $solutionPackagesFolderPath = $Env:NUGET_SOLUTION_PACKAGES_FOLDER_PATH

    # Cleanup if necessary
    If ($cleanGlobalPackagesFolder -Or $cleanHttpCache -Or $cleanPluginsCache)
    {
        If ($cleanGlobalPackagesFolder -And $cleanHttpCache -And $cleanPluginsCache)
        {
            $localsArguments = "all"
        }
        ElseIf ($cleanGlobalPackagesFolder -And $cleanHttpCache)
        {
            $localsArguments = "http-cache global-packages"
        }
        ElseIf ($cleanGlobalPackagesFolder)
        {
            $localsArguments = "global-packages"
        }
        ElseIf ($cleanHttpCache)
        {
            $localsArguments = "http-cache"
        }
        Else
        {
            Log "Too risky to invoke a locals clear with the specified parameters." "yellow"
        }

        If ($isClientDotnetExe)
        {
            . $nugetClientFilePath nuget locals -c $localsArguments *>>$null
        }
        Else
        {
            . $nugetClientFilePath locals -clear $localsArguments -Verbosity quiet
        }

        If ($isPackagesConfig -And ($cleanGlobalPackagesFolder -Or $cleanHttpCache))
        {
            Remove-Item $solutionPackagesFolderPath -Recurse -Force -ErrorAction Ignore > $Null
            mkdir $solutionPackagesFolderPath > $Null
        }
    }

    if($killMsBuildAndDotnetExeProcesses)
    {
        Stop-Process -name msbuild*,dotnet* -Force
    }

    $arguments = [System.Collections.Generic.List[string]]::new()

    $arguments.Add("restore")
    $arguments.Add($solutionFilePath)

    If ($isPackagesConfig)
    {
        If ($isClientDotnetExe)
        {
            $arguments.Add("--packages")
        }
        Else
        {
            $arguments.Add("-PackagesDirectory")
        }

        $arguments.Add($Env:NUGET_SOLUTION_PACKAGES_FOLDER_PATH)
    }

    If ($force)
    {
        If ($isClientDotnetExe)
        {
            $arguments.Add("--force")
        }
        Else
        {
            $arguments.Add("-Force")
        }
    }

    If (!$isClientDotnetExe)
    {
        $arguments.Add("-NonInteractive")
    }
    else
    {
        $arguments.Add("--verbosity")
        $arguments.Add("normal")
    }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    $logs = . $nugetClientFilePath $arguments

    $totalTime = $stopwatch.Elapsed.TotalSeconds
    $restoreStatistics = ExtractProjectRestoreStatistics $logs

    If ($Null -ne $restoreStatistics)
    {
        $restoreCount = $restoreStatistics.Count
        $restoreMaxTime = $restoreStatistics.Maximum.TotalSeconds
        $restoreSumTime = $restoreStatistics.Sum.TotalSeconds
        $restoreAvgTime = $restoreStatistics.Average.TotalSeconds
    }

    if(![string]::IsNullOrEmpty($logsFolderPath))
    {
        $logDate = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssffff")
        $solutionNoExt = [System.IO.Path]::GetFileNameWithoutExtension($solutionFilePath)
        if ($variantName) {
            $logFileName = "restoreLog-$variantName-$solutionNoExt-$logDate.txt"
        } else {
            $logFileName = "restoreLog-$solutionNoExt-$logDate.txt"
        }
        $logFile = [System.IO.Path]::Combine($logsFolderPath, $logFileName)
        OutFileWithCreateFolders $logFile ($logs | Out-String)
    }

    $folderPath = $Env:NUGET_PACKAGES
    $globalPackagesFolderNupkgFilesInfo = GetFilesInfo(GetPackageFiles $folderPath)
    $globalPackagesFolderFilesInfo = GetFilesInfo(GetFiles $folderPath)

    $folderPath = $Env:NUGET_HTTP_CACHE_PATH
    $httpCacheFilesInfo = GetFilesInfo(GetFiles $folderPath)

    $folderPath = $Env:NUGET_PLUGINS_CACHE_PATH
    $pluginsCacheFilesInfo = GetFilesInfo(GetFiles $folderPath)

    $clientName = GetClientName $nugetClientFilePath
    $clientVersion = GetClientVersion $nugetClientFilePath

    If (!(Test-Path $resultsFilePath))
    {
        $columnHeaders = "Machine Name,Client Name,Client Version,Solution Name,Timestamp (UTC),Iteration,Iteration Count,Scenario Name,Variant Name,Total Time (seconds),Project Restore Count,Max Project Restore Time (seconds),Sum Project Restore Time (seconds),Average Project Restore Time (seconds),Force," + `
            "Global Packages Folder .nupkg Count,Global Packages Folder .nupkg Size (MB),Global Packages Folder File Count,Global Packages Folder File Size (MB),Clean Global Packages Folder," + `
            "HTTP Cache File Count,HTTP Cache File Size (MB),Clean HTTP Cache,Plugins Cache File Count,Plugins Cache File Size (MB),Clean Plugins Cache,Kill MSBuild and dotnet Processes," + `
            "Processor Name,Processor Physical Core Count,Processor Logical Core Count,Log File Name"

        OutFileWithCreateFolders $resultsFilePath $columnHeaders
    }

    $data = "$($Env:COMPUTERNAME),$clientName,$clientVersion,$solutionName,$timestampUtc,$iteration,$iterationCount,$scenarioName,$variantName,$totalTime,$restoreCount,$restoreMaxTime,$restoreSumTime,$restoreAvgTime,$force," + `
        "$($globalPackagesFolderNupkgFilesInfo.Count),$($globalPackagesFolderNupkgFilesInfo.TotalSizeInMB),$($globalPackagesFolderFilesInfo.Count),$($globalPackagesFolderFilesInfo.TotalSizeInMB),$cleanGlobalPackagesFolder," + `
        "$($httpCacheFilesInfo.Count),$($httpCacheFilesInfo.TotalSizeInMB),$cleanHttpCache,$($pluginsCacheFilesInfo.Count),$($pluginsCacheFilesInfo.TotalSizeInMB),$cleanPluginsCache,$killMsBuildAndDotnetExeProcesses," + `
        "$($processorInfo.Name),$($processorInfo.NumberOfCores),$($processorInfo.NumberOfLogicalProcessors),$logFileName"

    Add-Content -Path $resultsFilePath -Value $data

    if ($dumpNupkgsPath) {
        Log "Copying .nupkg files to $dumpNupkgsPath."
        if (!(Test-Path $dumpNupkgsPath)) {
            New-Item $dumpNupkgsPath -Type Directory | Out-Null
        }
        
        $prefix = (Resolve-Path $Env:NUGET_PACKAGES).Path + '\'
        GetPackageFiles $Env:NUGET_PACKAGES `
            | ForEach-Object { (Resolve-Path $_.FullName).Path } `
            | ForEach-Object {
                $dest = Join-Path $dumpNupkgsPath $_.Substring($prefix.Length)
                $destDir = [IO.Path]::GetDirectoryName($dest)
                if (!(Test-Path $destDir)) {
                    New-Item $destDir -ItemType Directory | Out-Null
                }
                Copy-Item $_ $dest -Force
            }
    }

    Log "Finished measuring."
}