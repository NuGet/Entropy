<#
.SYNOPSIS
Downloads an SDK version and replaces the the NuGet files as part of that SDK with the here specified ones.

.PARAMETER SDKPath
The path where the patched SDK will be installed. It will be created it if doesn't exist.

.PARAMETER NupkgsPath
The nupkgs folder which contains the latest nupkgs.

.PARAMETER SDKChannel
Channel name of SDK. Please refer to https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script#options. 
Two-part version in A.B format, representing a specific release (for example, 6.0 or 7.0). 
Three-part version in A.B.Cxx format, representing a specific SDK release (for example, 5.0.1xx or 5.0.2xx). Available since the 5.0 release.

.PARAMETER Quality
The build quality. Works in conjunction with the channel. Likely options: `daily`, `preview` or `GA`, which represents daily, monthly and General Availability release respectively.
Please refer to https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script#options. 

.PARAMETER SkipPatching
Gives you the ability to skip patching, if you want to run the tests *against* a stable SDK version.

.EXAMPLE
.\SDKPatch.ps1 -SDKPath E:\SDK -NupkgsPath E:\NuGet\NuGet.Client\artifacts\nupkgs -SDKChannel 8.0.2xx -Quality daily
Use this to download the latest private build of a release, if any.

.EXAMPLE
.\SDKPatch.ps1 -SDKPath E:\SDK -NupkgsPath E:\NuGet\NuGet.Client\artifacts\nupkgs -SDKChannel 8.0.2xx -Quality preview
Use this to download the latest public preview of a release, if any.

.EXAMPLE
.\SDKPatch.ps1 -SDKPath E:\SDK -NupkgsPath E:\NuGet\NuGet.Client\artifacts\nupkgs -SDKChannel 8.0.2xx
Use this to download the latest GA version of a release, if any.

.EXAMPLE
.\SDKPatch.ps1 -SDKPath E:\SDK -SDKVersion 9.0.100-preview.2.24157.14
# Use this to download the a specific version of a release.

.EXAMPLE
.\SDKPatch.ps1 -SDKPath E:\SDK -SDKChannel 8.0.2xx -Quality GA -SkipPatching
# Use this to download the latest GA version of a release without patching.

#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $True)]
    [string]$SDKPath,
    [Parameter(Mandatory = $True)]
    [string]$NupkgsPath,
    [string]$SDKVersion,
    [string]$SDKChannel,
    [string]$Quality,
    [switch]$SkipPatching
)
# NupkgsPath is the nupkgs folder which contains the latest nupkgs.
# Channel name of SDK. Pls refer to https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script#options
  # Two-part version in A.B format, representing a specific release (for example, 6.0 or 7.0).
  # Three-part version in A.B.Cxx format, representing a specific SDK release (for example, 5.0.1xx or 5.0.2xx). Available since the 5.0 release.

if(!($SkipPatching)) # Only check the nupkgs path if we're patching
{
    if(!(Test-Path -Path $NupkgsPath))
    {
        Write-Error "The nupkgs path does not exist: $NupkgsPath"
        exit
    }
}

$DownloadSpecificVersion = $false

if (-not([string]::IsNullOrEmpty($SDKVersion)))
{
    $DownloadSpecificVersion = $true
}
else
{
    $SDKVersion = "latest"
}

if ([string]::IsNullOrEmpty($Quality)) 
{
    $Quality = "GA"
}

function PatchNupkgs {
    param(
        [Parameter(Mandatory = $true)]
        [string]$nupkgId,
        [Parameter(Mandatory = $true)]
        [string]$suffix,
        [Parameter(Mandatory = $true)]
        [string]$tempFolder,
        [Parameter(Mandatory = $true)]
        [string]$patchSDKFolder,
        [Parameter(Mandatory = $true)]
        [string]$SDKVersion,
        [Parameter(Mandatory = $true)]
        [string]$nupkgsPath
    )

    #Create a temp folder for extracted files
    $tempExtractFolder = [System.IO.Path]::Combine($tempFolder, $nupkgID)
    $delimeter = [IO.Path]::DirectorySeparatorChar

    if (Test-Path $tempExtractFolder) {
        Remove-Item -Path "$tempExtractFolder$delimeter*" -Force -Recurse
    }else{
        New-Item $tempExtractFolder -ItemType Directory | Out-Null
    }
    
    #Copy the nupkg from nuget artifacts nupkg folder, to the temp folder, and extracted it
    $nupkg = Get-ChildItem -Path "$nupkgsPath" -Exclude "*.symbols.nupkg" | Where-Object {$_.Name -like "$nupkgId$suffix*"}

    if(-not $nupkg -Or -not(Test-Path $nupkg))
    {
        Write-Error "$nupkgId$suffix not found in $nupkgsPath"
        return $false
    }

    Copy-Item $nupkg -Destination $tempExtractFolder

    $nupkgTemp = [System.IO.Path]::Combine($tempExtractFolder, $nupkg.Name)
    
    if (!(Test-Path $nupkgTemp)) {
        Start-Sleep -s 1
    }

    $zip = Rename-Item -Path $nupkgTemp -NewName ($nupkgTemp -replace "nupkg", "zip") -PassThru

    Expand-Archive -LiteralPath $zip.FullName -DestinationPath $tempExtractFolder

    #Patch dll
    if ($nupkgId -eq "NuGet.Build.Tasks.Console") {
        $libPath = [System.IO.Path]::Combine($tempExtractFolder, 'contentFiles', 'any')
    }else{
        $libPath = [System.IO.Path]::Combine($tempExtractFolder, 'lib')
    }
    $tfmFolderNet9 =  Get-ChildItem -Path "$libPath$delimeter*" | Where-Object {$_.Name -like "net9.0" }

    $tfmFolderNet8 =  Get-ChildItem -Path "$libPath$delimeter*" | Where-Object {$_.Name -like "net8.0" }

    $tfmFolderNet7 =  Get-ChildItem -Path "$libPath$delimeter*" | Where-Object {$_.Name -like "net7.0" }
    
    $tfmFolderNet5 =  Get-ChildItem -Path "$libPath$delimeter*" | Where-Object {$_.Name -like "net5.0" }
   
    $tfmFolderNetcoreapp50 =  Get-ChildItem -Path "$libPath$delimeter*" | Where-Object {$_.Name -like "netcoreapp5.0"}
   
    $tfmFolderNetstandard20 =  Get-ChildItem -Path "$libPath$delimeter*" | Where-Object {$_.Name -like "netstandard2.0"}
    
    $tfmFolderNetcoreapp21 =  Get-ChildItem -Path "$libPath$delimeter*" | Where-Object {$_.Name -like "netcoreapp2.1"}
    
    if (([int]($SDKVersion.Split('.', [System.StringSplitOptions]::RemoveEmptyEntries)[0]) -ge 5) -And ($null -ne $tfmFolderNet9)){
        $patchDll = Get-ChildItem -Path "$tfmFolderNet9$delimeter*" | Where-Object {$_.Name -like "*.dll"}
    }
    elseif (([int]($SDKVersion.Split('.', [System.StringSplitOptions]::RemoveEmptyEntries)[0]) -ge 5) -And ($null -ne $tfmFolderNet8)){
            $patchDll = Get-ChildItem -Path "$tfmFolderNet8$delimeter*" | Where-Object {$_.Name -like "*.dll"}
    }
    elseif (([int]($SDKVersion.Split('.', [System.StringSplitOptions]::RemoveEmptyEntries)[0]) -ge 5) -And ($null -ne $tfmFolderNet7)){
            $patchDll = Get-ChildItem -Path "$tfmFolderNet7$delimeter*" | Where-Object {$_.Name -like "*.dll"}
    }
    elseif (([int]($SDKVersion.Split('.', [System.StringSplitOptions]::RemoveEmptyEntries)[0]) -ge 5) -And (($tfmFolderNet5 -ne $null) -Or ($tfmFolderNetcoreapp50 -ne $null))){

        if ($tfmFolderNet5 -ne $null){
            $patchDll = Get-ChildItem -Path "$tfmFolderNet5$delimeter*" | Where-Object {$_.Name -like "*.dll"}
        }else{
            $patchDll = Get-ChildItem -Path "$tfmFolderNetcoreapp50$delimeter*" | Where-Object {$_.Name -like "*.dll"}
        }
    }elseif (($tfmFolderNetstandard20 -ne $null) -Or ($tfmFolderNetcoreapp21 -ne $null)){
        if ($tfmFolderNetstandard20 -ne $null){
            $patchDll = Get-ChildItem -Path "$tfmFolderNetstandard20$delimeter*" | Where-Object {$_.Name -like "*.dll"}
        }else{
            $patchDll = Get-ChildItem -Path "$tfmFolderNetcoreapp21$delimeter*" | Where-Object {$_.Name -like "*.dll"}
        }
    }else{
        write-error "No dlls found in any TFM folder!"
        return $false
    }
    Write-Host "Dll :  $patchDll will be used for patching."

    #the destination of the dlls in nupkg should be dotnet/sdk/x.yz/
    $destPath = [System.IO.Path]::Combine($patchSDKFolder, 'sdk', $SDKVersion)
    $dllName = $patchDll.Name

    $destDllPath = [System.IO.Path]::Combine($destPath, $dllName)
    if (Test-Path $destDllPath)
    {
        Remove-Item -Path "$destDllPath"  -Force
        if (Test-Path $destDllPath){
            write-error "Dll $dllName could not be deleted from $patchSDKFolder!"
            return $false
        }    
    }

    if((Test-Path $destDllPath) -Or -not($nupkgId -eq "NuGet.Packaging.Core")){

        Copy-Item "$patchDll" -Destination "$destPath" 

        if (-not(Test-Path $destDllPath)) {
            write-error "Dll $dllName was not copied to $patchSDKFolder!"
            return $false
        }
    }

    #patch NuGet.targets and NuGet.props
    if ($nupkgId -eq "NuGet.Build.Tasks"){
        
        $sourceTargetsPath = [System.IO.Path]::Combine($tempExtractFolder, 'runtimes', 'any', 'native', 'NuGet.targets')
        $destTargetsPath = [System.IO.Path]::Combine($destPath, 'NuGet.targets')

        if (Test-Path $sourceTargetsPath){
            Remove-Item -Path "$destTargetsPath" -Force
            if (Test-Path $destTargetsPath){
                write-error "NuGet.Targets could not be deleted from $patchSDKFolder!"
                return false
            }else{
                Copy-Item "$sourceTargetsPath" -Destination "$destTargetsPath"
                if (-not (Test-Path $destTargetsPath)){
                    write-error "NuGet.Targets was not copied to $patchSDKFolder!"
                    return $false
                }
            }
            Write-Host "NuGet.targets :  $sourceTargetsPath will be used for patching."
        }

    
        $sourcePropsPath = [System.IO.Path]::Combine($tempExtractFolder, 'runtimes', 'any', 'native', 'NuGet.props')
        $destPropsPath = [System.IO.Path]::Combine($destPath, 'NuGet.props')

        if (Test-Path $sourcePropsPath){
            Remove-Item -Path "$destPropsPath" -Force
            if (Test-Path $destPropsPath){
                write-error "NuGet.props could not be deleted from $patchSDKFolder!"
                return false
            }else{
                Copy-Item "$sourcePropsPath" -Destination "$destPropsPath"
                if (-not (Test-Path $destPropsPath)){
                    write-error "NuGet.props was not copied to $patchSDKFolder!"
                    return $false
                }
            }
            Write-Host "NuGet.props :  $sourcePropsPath will be used for patching."
        }
    }
    

    #patch NuGet.REstoreEx.targets
    if ($nupkgId -eq "NuGet.Build.Tasks.Console"){
        $sourceREstoreExPath = [System.IO.Path]::Combine($tempExtractFolder, 'runtimes', 'any', 'native', 'NuGet.REstoreEx.targets')
        $destREstoreExPath = [System.IO.Path]::Combine($destPath, 'NuGet.REstoreEx.targets')

        if (Test-Path $sourceREstoreExPath){
            Remove-Item -Path "$destREstoreExPath" -Force
            if (Test-Path $destREstoreExPath){
                write-error "NuGet.REstoreEx.targets could not be deleted from $patchSDKFolder!"
                return false
            }else{
                Copy-Item "$sourceREstoreExPath" -Destination "$destREstoreExPath"
                if (-not (Test-Path $destREstoreExPath)){
                    write-error "NuGet.REstoreEx.targets was not copied to $patchSDKFolder!"
                    return $false
                }
            }
            Write-Host "NuGet.REstoreEx.targets :  $sourceREstoreExPath will be used for patching."
        }
    }
    

    return $true   
}

function PatchPackNupkg{
    param(
        [Parameter(Mandatory = $true)]
        [string]$nupkgId,
        [Parameter(Mandatory = $true)]
        [string]$suffix,
        [Parameter(Mandatory = $true)]
        [string]$tempFolder,
        [Parameter(Mandatory = $true)]
        [string]$patchSDKFolder,
        [Parameter(Mandatory = $true)]
        [string]$SDKVersion,
        [Parameter(Mandatory = $true)]
        [string]$nupkgsPath
    )
    $delimeter = [IO.Path]::DirectorySeparatorChar

    #Create a temp folder for extracted contents
    $tempExtractFolder = [System.IO.Path]::Combine($tempFolder, $nupkgID)

    if (Test-Path $tempExtractFolder) {
        Remove-Item -Path "$tempExtractFolder" -Force -Recurse
    }
    New-Item $tempExtractFolder -ItemType Directory | Out-Null

    #Copy the nupkg from nuget artifacts nupkg folder, to the temp folder, and extracted it
    $nupkg = Get-ChildItem -Path "$nupkgsPath" -Exclude "*.symbols.nupkg" | Where-Object {$_.Name -like "$nupkgId$suffix*"}
    
    Copy-Item $nupkg -Destination $tempExtractFolder

    $nupkgTemp = [System.IO.Path]::Combine($tempExtractFolder, $nupkg.Name)
    while (!(Test-Path $nupkgTemp))
     {
        Start-Sleep -s 1
     }

    $zip = Rename-Item -Path $nupkgTemp -NewName ($nupkgTemp -replace "nupkg", "zip") -PassThru

    Expand-Archive -LiteralPath $zip.FullName -DestinationPath $tempExtractFolder

    #Prepare for the destination folder , that is : dotnet/sdk/5.xx/Sdks/NuGet.Build.Tasks.Pack
    $destPath = [System.IO.Path]::Combine($patchSDKFolder, 'sdk', $SDKVersion, 'Sdks', 'NuGet.Build.Tasks.Pack')

    Remove-Item -Path "$destPath$delimeter*" -Force -Recurse
    if ((Get-ChildItem -Path "$destPath") -ne $null){
        write-error "Folders could not be deleted from $destPath!"
        return $false
    }

    #Copy the build, buildCrossTargeting, CoreCLR, Desktop folders from extracted folder to the destination folder
    Copy-Item -Path "$tempExtractFolder$delimeter*"  -Include @("build", "buildCrossTargeting", "CoreCLR", "Desktop") -Destination "$destPath" -Recurse

    if ((Get-ChildItem -Path "$destPath") -eq $null){
        write-error "Folders were not copied to $patchSDKFolder!"
        return $false
    }
    Write-Host "NuGet.Build.Tasks.Pack :  $tempExtractFolder will be used for patching."
    return $true   
}


function Patch
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$patchSDKFolder,
        [Parameter(Mandatory = $true)]
        [string]$SDKVersion,
        [Parameter(Mandatory = $true)]
        [string]$nupkgsPath
        )

    $packNupkgId = "NuGet.Build.Tasks.Pack"
    $copiedNupkgIds = @(
        "Microsoft.Build.NuGetSdkResolver",
        "NuGet.Build.Tasks.Console",
        "NuGet.Build.Tasks", 
        "NuGet.Versioning", 
        "NuGet.Protocol", 
        "NuGet.ProjectModel", 
        "NuGet.Packaging", 
        "NuGet.LibraryModel", 
        "NuGet.Frameworks", 
        "NuGet.DependencyResolver.Core", 
        "NuGet.Configuration", 
        "NuGet.Common", 
        "NuGet.Commands", 
        "NuGet.CommandLine.XPlat", 
        "NuGet.Credentials")

    if (([int]($SDKVersion.Split('.', [System.StringSplitOptions]::RemoveEmptyEntries)[0]) -le 7) )
    {
        Write-Host "Adding nuget.packaging.core"
        $copiedNupkgIds += "NuGet.Packaging.Core"
    }

    $packNupkg = Get-ChildItem -Path "$nupkgsPath" -Exclude '*.symbols.nupkg'| Where-Object {$_.Name -like "NuGet.Build.Tasks.Pack*"}
    $suffix = $packNupkg.Name -replace "NuGet.Build.Tasks.Pack", ""

    $tempdir = [System.IO.Path]::GetTempPath()
    $tempFolder = [System.IO.Path]::Combine($tempdir, 'PatchTempFolder')

    if (Test-Path $tempFolder) {
        Remove-Item -Path "$tempFolder$delimeter*" -Force -Recurse
    }else{
        New-Item $tempFolder -ItemType Directory | Out-Null
    }
    

    Write-Host "PatchPackNupkg -nupkgId $packNupkgId -suffix $suffix -tempFolder $tempFolder -patchSDKFolder $patchSDKFolder -SDKVersion $SDKVersion -nupkgsPath $nupkgsPath"
    $result = PatchPackNupkg -nupkgId $packNupkgId -suffix $suffix -tempFolder $tempFolder -patchSDKFolder $patchSDKFolder -SDKVersion $SDKVersion -nupkgsPath $nupkgsPath
    if ($result -eq $true){
        Write-Host "Patched NuGet.Build.Tasks.Pack successfully. `n"
    }else{
        write-error "Failed to patch NuGet.Build.Tasks.Pack! `n"
        return $false
    }

    foreach ($nupkgId in $copiedNupkgIds) {
        #Write-Host "PatchNupkgs -nupkgId $nupkgId -suffix $suffix -tempFolder $tempFolder -patchSDKFolder $patchSDKFolder -SDKVersion $SDKVersion -nupkgsPath $nupkgsPath"
        $result = PatchNupkgs -nupkgId $nupkgId -suffix $suffix -tempFolder $tempFolder -patchSDKFolder $patchSDKFolder -SDKVersion $SDKVersion -nupkgsPath $nupkgsPath
        if ($result -eq $true){
            Write-Host "Patched $nupkgId successfully.`n"
        }else{
            write-error "Failed to patch $nupkgId! `n"
            return $false
        }
    }
        
    #Remove-Item -Path "$tempFolder" -Force -Recurse
    return $true
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

if (!(Test-Path $SDKPath)) 
{
    New-Item $SDKPath -ItemType Directory | Out-Null
}

if ("Win32NT" -eq [System.Environment]::OSVersion.Platform)
{
    if (!(Test-Path $SDKPath\dotnet-install.ps1)) 
    {
        Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile $SDKPath\dotnet-install.ps1
    }

    if($DownloadSpecificVersion)
    {
        & $SDKPath\dotnet-install.ps1 -InstallDir $SDKPath -Version $SDKVersion -NoPath
    }
    else 
    {
        & $SDKPath\dotnet-install.ps1 -InstallDir $SDKPath -Channel $SDKChannel -Version $SDKVersion -Quality $Quality -NoPath
    }

    $DOTNET = Join-Path -Path $SDKPath -ChildPath 'dotnet.exe'
} 
else 
{
    if (!(Test-Path $SDKPath/dotnet-install.sh)) 
    {
        Invoke-WebRequest https://dot.net/v1/dotnet-install.sh -OutFile $SDKPath/dotnet-install.sh
    }

    sudo chmod u+x $SDKPath/dotnet-install.sh

    if($DownloadSpecificVersion)
    {
        & $SDKPath/dotnet-install.sh -InstallDir $SDKPath -Version $SDKVersion -NoPath
    }
    else 
    {
        & $SDKPath/dotnet-install.sh -InstallDir $SDKPath -Channel $SDKChannel -Version $SDKVersion -Quality $Quality -NoPath
    }

   
    $DOTNET = Join-Path -Path $SDKPath -ChildPath 'dotnet'
}

# Set DOTNET_MULTILEVEL_LOOKUP to 0 so it will just check the version in the specific path.
$env:DOTNET_MULTILEVEL_LOOKUP = 0
# Display current version
& $DOTNET --version
$SDKVersion = & $DOTNET --version

if(!($SkipPatching)) 
{
    $result = Patch $SDKPath $SDKVersion $NupkgsPath

    if ($result -eq $true)
    {
        Write-Host "Finish patching `n"
    }else{
        Write-Host "Patching failed `n"
    }
} 
else 
{
    Write-Host "Skipped patching `n"
}
