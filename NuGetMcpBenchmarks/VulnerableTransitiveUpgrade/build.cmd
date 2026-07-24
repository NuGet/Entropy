@echo off
setlocal
set FEED=%~dp0local-feed
if not exist "%FEED%" mkdir "%FEED%"
del /q "%FEED%\*.nupkg" 2>nul

REM Pack Core first (DataPipeline depends on it from the local feed).
dotnet pack feed-src\Contoso.Internal.Core -c Release -p:PackageVersion=1.4.0 -p:NsjVersion=12.0.3 -o "%FEED%" || exit /b 1
dotnet pack feed-src\Contoso.Internal.Core -c Release -p:PackageVersion=2.0.0 -p:NsjVersion=13.0.3 -o "%FEED%" || exit /b 1

REM Pack DataPipeline (restores Core from the local feed via NuGet.Config).
dotnet pack feed-src\Contoso.Internal.DataPipeline -c Release -p:PackageVersion=2.3.1 -p:CoreVersion=1.4.0 -o "%FEED%" || exit /b 1

echo Local feed built at %FEED%.
