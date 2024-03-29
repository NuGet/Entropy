msbuild Packages\Packages.sln /t:clean /p:Configuration=Release
msbuild Projects\Projects.sln /t:clean /p:Configuration=Release
msbuild Projects\CPVM01\CPVM01.sln /t:clean /p:Configuration=Release
rmdir /Q /S NuGetArtifacts
rmdir /Q /S NuGetArtifactsInstalled
powershell "Get-ChildItem -Path . -Include bin -Recurse | foreach ($_) { remove-item $_.fullname -Force -Recurse }"
powershell "Get-ChildItem -Path . -Include obj -Recurse | foreach ($_) { remove-item $_.fullname -Force -Recurse }"