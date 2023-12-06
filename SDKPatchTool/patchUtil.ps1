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

    $tfmFolderNet7 =  Get-ChildItem -Path "$libPath$delimeter*" | Where-Object {$_.Name -like "net7.0" }
    
    $tfmFolderNet5 =  Get-ChildItem -Path "$libPath$delimeter*" | Where-Object {$_.Name -like "net5.0" }
   
    $tfmFolderNetcoreapp50 =  Get-ChildItem -Path "$libPath$delimeter*" | Where-Object {$_.Name -like "netcoreapp5.0"}
   
    $tfmFolderNetstandard20 =  Get-ChildItem -Path "$libPath$delimeter*" | Where-Object {$_.Name -like "netstandard2.0"}
    
    $tfmFolderNetcoreapp21 =  Get-ChildItem -Path "$libPath$delimeter*" | Where-Object {$_.Name -like "netcoreapp2.1"}
   
    if (([int]($SDKVersion.Substring(0, 1)) -ge 5) -And ($null -ne $tfmFolderNet7)){
        $patchDll = Get-ChildItem -Path "$tfmFolderNet7$delimeter*" | Where-Object {$_.Name -like "*.dll"}
    }
    elseif (([int]($SDKVersion.Substring(0, 1)) -ge 5) -And (($tfmFolderNet5 -ne $null) -Or ($tfmFolderNetcoreapp50 -ne $null))){

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
    Write-host "Dll :  $patchDll will be used for patching."

    #the destination of the dlls in nupkg should be dotnet/sdk/x.yz/
    $destPath = [System.IO.Path]::Combine($patchSDKFolder, 'sdk', $SDKVersion)
    $dllName = $patchDll.Name

    $destDllPath = [System.IO.Path]::Combine($destPath, $dllName)
    Remove-Item -Path "$destDllPath"  -Force
    if (Test-Path $destDllPath){
        write-error "Dll $dllName could not be deleted from $patchSDKFolder!"
        return $false
    }

    Copy-Item "$patchDll" -Destination "$destPath" 
    if (-not(Test-Path $destDllPath)) {
        write-error "Dll $dllName was not copied to $patchSDKFolder!"
        return $false
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
            Write-host "NuGet.targets :  $sourceTargetsPath will be used for patching."
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
            Write-host "NuGet.props :  $sourcePropsPath will be used for patching."
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
            Write-host "NuGet.REstoreEx.targets :  $sourceREstoreExPath will be used for patching."
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
    Write-host "NuGet.Build.Tasks.Pack :  $tempExtractFolder will be used for patching."
    return $true   
}


function Patch{
        [string]$patchSDKFolder,
        [string]$SDKVersion,
        [string]$nupkgsPath
    $packNupkgId = "NuGet.Build.Tasks.Pack"
    $copiedNupkgIds = @(
        "Microsoft.Build.NuGetSdkResolver",
        "NuGet.Build.Tasks.Console",
        "NuGet.Build.Tasks", 
        "NuGet.Versioning", 
        "NuGet.Protocol", 
        "NuGet.ProjectModel", 
        "NuGet.Packaging", 
        "NuGet.Packaging.Core", 
        "NuGet.LibraryModel", 
        "NuGet.Frameworks", 
        "NuGet.DependencyResolver.Core", 
        "NuGet.Configuration", 
        "NuGet.Common", 
        "NuGet.Commands", 
        "NuGet.CommandLine.XPlat", 
        "NuGet.Credentials")

    $packNupkg = Get-ChildItem -Path "$nupkgsPath" -Exclude '*.symbols.nupkg'| Where-Object {$_.Name -like "NuGet.Build.Tasks.Pack*"}
    $suffix = $packNupkg.Name -replace "NuGet.Build.Tasks.Pack", ""

    $tempdir = [System.IO.Path]::GetTempPath()
    $tempFolder = [System.IO.Path]::Combine($tempdir, 'PatchTempFolder')

    if (Test-Path $tempFolder) {
        Remove-Item -Path "$tempFolder$delimeter*" -Force -Recurse
    }else{
        New-Item $tempFolder -ItemType Directory | Out-Null
    }
    

    #Write-host "PatchPackNupkg -nupkgId $packNupkgId -suffix $suffix -tempFolder $tempFolder -patchSDKFolder $patchSDKFolder -SDKVersion $SDKVersion -nupkgsPath $nupkgsPath"
    $result = PatchPackNupkg -nupkgId $packNupkgId -suffix $suffix -tempFolder $tempFolder -patchSDKFolder $patchSDKFolder -SDKVersion $SDKVersion -nupkgsPath $nupkgsPath
    if ($result -eq $true){
        write-host "Patched NuGet.Build.Tasks.Pack successfully. `n"
    }else{
        write-error "Failed to patch NuGet.Build.Tasks.Pack! `n"
        return $false
    }

    foreach ($nupkgId in $copiedNupkgIds) {
        #Write-host "PatchNupkgs -nupkgId $nupkgId -suffix $suffix -tempFolder $tempFolder -patchSDKFolder $patchSDKFolder -SDKVersion $SDKVersion -nupkgsPath $nupkgsPath"
        $result = PatchNupkgs -nupkgId $nupkgId -suffix $suffix -tempFolder $tempFolder -patchSDKFolder $patchSDKFolder -SDKVersion $SDKVersion -nupkgsPath $nupkgsPath
        if ($result -eq $true){
            write-host "Patched $nupkgId successfully.`n"
        }else{
            write-error "Failed to patch $nupkgId! `n"
            return $false
        }
    }
        
    #Remove-Item -Path "$tempFolder" -Force -Recurse
    return $true
}