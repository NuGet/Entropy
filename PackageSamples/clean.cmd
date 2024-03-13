msbuild Packages\Packages.sln /t:clean /p:Configuration=Release
msbuild Projects\Projects.sln /t:clean /p:Configuration=Release
rmdir /Q /S NuGetArtifacts
rmdir /Q /S NuGetArtifactsInstalled