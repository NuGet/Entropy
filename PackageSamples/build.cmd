mkdir NuGetArtifacts
mkdir NuGetArtifactsInstalled
msbuild Packages\Packages.sln /t:Restore /p:Configuration=Release
msbuild Packages\Packages.sln /p:Configuration=Release
msbuild Packages\Packages.sln /t:Restore /p:Configuration=Release
msbuild Packages\Packages.sln /p:Configuration=Release
msbuild Projects\Projects.sln /t:Restore /p:Configuration=Release
msbuild Projects\Projects.sln /p:Configuration=Release
msbuild Projects\CPVM01\CPVM01.sln /t:Restore /p:Configuration=Release
msbuild Projects\CPVM01\CPVM01.sln /p:Configuration=Release