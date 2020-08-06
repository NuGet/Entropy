msbuild /t:build /p:DebugSymbols=true /p:DebugType=full -r /p:Configuration=release
msbuild /t:PublishAndILMerge /p:DebugSymbols=true /p:DebugType=full /p:Configuration=release
dotnet pack --no-build /p:DebugSymbols=true /p:DebugType=full /p:Configuration=release