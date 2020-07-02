# patchSDKFolder is the folder stores the patched SDK. It will be created if it doesn't exist.
$patchSDKFolder = "C:\PatchedSDK"
# nupkgsPath is the nupkgs folder which contains the latest nupkgs.
$nupkgsPath = "C:\repos\NuGet.Client\artifacts\nupkgs"

. ".\patchUtil.ps1"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

if (!(Test-Path $patchSDKFolder)) {
    New-Item $patchSDKFolder -ItemType Directory | Out-Null
}
if (!(Test-Path $patchSDKFolder\dotnet-install.ps1)) {
    Invoke-WebRequest https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.ps1 -OutFile $patchSDKFolder\dotnet-install.ps1
}

& $patchSDKFolder\dotnet-install.ps1 -i $patchSDKFolder -c master -v latest -NoPath

        
$DOTNET = Join-Path -Path $patchSDKFolder -ChildPath 'dotnet'
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