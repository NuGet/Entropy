# patchSDKFolder is the folder stores the patched SDK. It will be created if it doesn't exist.
$patchSDKFolder = "/home/henli/patchSDK"

# nupkgsPath is the nupkgs folder which contains the latest nupkgs.
$nupkgsPath = "/home/henli/Nupkgs"

# SDKVersion is the version of dotnet/sdk which NuGet is inserting into.
$SDKVersion = "5.0.100"

. "./patchUtil.ps1"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12


if (!(Test-Path $patchSDKFolder)) {
      New-Item $patchSDKFolder -ItemType Directory | Out-Null
}
if (!(Test-Path $patchSDKFolder/dotnet-install.sh)) {
    Invoke-WebRequest https://dot.net/v1/dotnet-install.sh -OutFile $patchSDKFolder/dotnet-install.sh
}

sudo chmod u+x $patchSDKFolder/dotnet-install.sh
& $patchSDKFolder/dotnet-install.sh -i $patchSDKFolder -c master -v $SDKVersion -NoPath

        
$DOTNET = Join-Path -Path $patchSDKFolder -ChildPath 'dotnet'
# Display current version
& $DOTNET --version
$DownloadedSDKVersion = & $DOTNET --version

$result = Patch -patchSDKFolder $patchSDKFolder -SDKVersion $DownloadedSDKVersion -nupkgsPath $nupkgsPath

if ($result -eq $true)
{
    write-host "Finish patching `n"
}else{
    write-host "Patching failed `n"
}