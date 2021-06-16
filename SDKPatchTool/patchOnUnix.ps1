# patchSDKFolder is the folder stores the patched SDK. It will be created if it doesn't exist.
$patchSDKFolder = "/Users/heng/patchSDK"

# nupkgsPath is the nupkgs folder which contains the latest nupkgs.
$nupkgsPath = "/Users/heng/Nupkgs"

# SDKVersion is the version of dotnet/sdk which NuGet is inserting into.
$SDKVersion = "latest"

# Channel name of SDK. Pls refer to https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script#options
# "Current" - Most current release (For now, it's 5.x)
# "main" - Branch name of a preview channel (For now, it's 6.x)
$SDKChannel = "main"

. "./patchUtil.ps1"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12


if (!(Test-Path $patchSDKFolder)) {
      New-Item $patchSDKFolder -ItemType Directory | Out-Null
}
if (!(Test-Path $patchSDKFolder/dotnet-install.sh)) {
    Invoke-WebRequest https://dot.net/v1/dotnet-install.sh -OutFile $patchSDKFolder/dotnet-install.sh
}

sudo chmod u+x $patchSDKFolder/dotnet-install.sh
& $patchSDKFolder/dotnet-install.sh -InstallDir $patchSDKFolder -Channel $SDKChannel -Version $SDKVersion -NoPath

        
$DOTNET = Join-Path -Path $patchSDKFolder -ChildPath 'dotnet'
# Set DOTNET_MULTILEVEL_LOOKUP to 0 so it will just check the version in the specific path.
$env:DOTNET_MULTILEVEL_LOOKUP = 0
# Display current version
& $DOTNET --version
$SDKVersion = & $DOTNET --version

$result = Patch -patchSDKFolder $patchSDKFolder -SDKVersion $SDKVersion -nupkgsPath $nupkgsPath

if ($result -eq $true)
{
    write-host "Finish patching `n"
}else{
    write-host "Patching failed `n"
}
