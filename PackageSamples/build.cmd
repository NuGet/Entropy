mkdir NuGetArtifacts
mkdir NuGetArtifactsInstalled

msbuild Packages\A\A.csproj /t:Restore /p:Configuration=Release
msbuild Packages\A\A.csproj /p:Configuration=Release

msbuild Packages\B\B.csproj /t:Restore /p:Configuration=Release
msbuild Packages\B\B.csproj /p:Configuration=Release

msbuild Packages\C\C.csproj /t:Restore /p:Configuration=Release
msbuild Packages\C\C.csproj /p:Configuration=Release

msbuild Packages\D\D.csproj /t:Restore /p:Configuration=Release
msbuild Packages\D\D.csproj /p:Configuration=Release

msbuild Packages\F_V1\F_V1.csproj /t:Restore /p:Configuration=Release
msbuild Packages\F_V1\F_V1.csproj /p:Configuration=Release

msbuild Packages\F_V2\F_V2.csproj /t:Restore /p:Configuration=Release
msbuild Packages\F_V2\F_V2.csproj /p:Configuration=Release

msbuild Packages\E_V2\E_V2.csproj /t:Restore /p:Configuration=Release
msbuild Packages\E_V2\E_V2.csproj /p:Configuration=Release

msbuild Packages\G_V2\G_V2.csproj /t:Restore /p:Configuration=Release
msbuild Packages\G_V2\G_V2.csproj /p:Configuration=Release

msbuild Packages\H\H.csproj /t:Restore /p:Configuration=Release
msbuild Packages\H\H.csproj /p:Configuration=Release

msbuild Packages\I\I.csproj /t:Restore /p:Configuration=Release
msbuild Packages\I\I.csproj /p:Configuration=Release

msbuild Packages\J\J.csproj /t:Restore /p:Configuration=Release
msbuild Packages\J\J.csproj /p:Configuration=Release

msbuild Packages\K\K.csproj /t:Restore /p:Configuration=Release
msbuild Packages\K\K.csproj /p:Configuration=Release

msbuild Packages\L_V1\L_V1.csproj /t:Restore /p:Configuration=Release
msbuild Packages\L_V1\L_V1.csproj /p:Configuration=Release

msbuild Packages\L_V2\L_V2.csproj /t:Restore /p:Configuration=Release
msbuild Packages\L_V2\L_V2.csproj /p:Configuration=Release

msbuild Packages\M_V1\M_V1.csproj /t:Restore /p:Configuration=Release
msbuild Packages\M_V1\M_V1.csproj /p:Configuration=Release

msbuild Packages\M_V2\M_V2.csproj /t:Restore /p:Configuration=Release
msbuild Packages\M_V2\M_V2.csproj /p:Configuration=Release

msbuild Packages\N\N.csproj /t:Restore /p:Configuration=Release
msbuild Packages\N\N.csproj /p:Configuration=Release

msbuild Projects\Projects.sln /t:Restore /p:Configuration=Release
msbuild Projects\Projects.sln /p:Configuration=Release

msbuild Projects\CPVM01\CPVM01.sln /t:Restore /p:Configuration=Release
msbuild Projects\CPVM01\CPVM01.sln /p:Configuration=Release