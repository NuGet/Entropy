# NuGetBuildValidators

## Introduction
This is a tool used to validate the localized strings for NuGet.Tools.vsix

## Instructions

* git clone https://github.com/mishra14/NuGetBuildValidators.git
* cd NuGetBuildValidators
* `msbuild`
* `.\bin\Debug\NuGetStringChecker.exe "Path\to\vsix\NuGet.Tools.vsix" "Path\to\extract\NuGet.Tools.Vsix" "Path\to\log\errors.txt"`
