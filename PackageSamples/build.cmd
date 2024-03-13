msbuild Packages\Packages.sln /p:Configuration=Release
msbuild Projects\Projects.sln /t:Restore /p:Configuration=Release
msbuild Projects\Projects.sln /p:Configuration=Release