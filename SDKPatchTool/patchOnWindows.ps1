# patchSDKFolder is the folder stores the patched SDK. It will be created if it doesn't exist.
$patchSDKFolder = "C:\PatchedSDK"

# nupkgsPath is the nupkgs folder which contains the latest nupkgs.
$nupkgsPath = "C:\repos\NuGet.Client\artifacts\nupkgs"

# SDKVersion is the version of dotnet/sdk which NuGet is inserting into.
$SDKVersion = "latest"

# Channel name of SDK. Pls refer to https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script#options
# Two-part version in A.B format, representing a specific release (for example, 6.0 or 7.0).
# Three-part version in A.B.Cxx format, representing a specific SDK release (for example, 5.0.1xx or 5.0.2xx). Available since the 5.0 release.
# If we'd like to test against lastest dotnet sdk NuGet inserted, we may refer to NuGet insertion PR to look for version number.
$SDKChannel = "7.0"

. ".\patchUtil.ps1"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

if (!(Test-Path $patchSDKFolder)) {
    New-Item $patchSDKFolder -ItemType Directory | Out-Null
}
if (!(Test-Path $patchSDKFolder\dotnet-install.ps1)) {
    Invoke-WebRequest https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.ps1 -OutFile $patchSDKFolder\dotnet-install.ps1
}

& $patchSDKFolder\dotnet-install.ps1 -InstallDir $patchSDKFolder -Channel $SDKChannel -Version $SDKVersion -NoPath

        
$DOTNET = Join-Path -Path $patchSDKFolder -ChildPath 'dotnet.exe'
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