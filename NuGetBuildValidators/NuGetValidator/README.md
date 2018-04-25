# NuGetValidator

## Introduction

This is a console application used to validate the localized strings for NuGet.Tools.vsix or NuGet.Client repository. This project uses the libraries from NuGetValidator.Localization to allow a better User Experience.


## Usage

### Using NuGetValidator.exe to validate NuGet.Client repository

The NuGetValidator.exe accepts the following arguments - 

```
PS> NuGetValidator.exe localization -h
nugetvalidator Version: 1.0.0.0


Usage: nugetvalidator localization [options]

Options:
  -h|--help               Show help information
  -x|--vsix               Switch to indicate that a vsix needs to be validate. If -x|--vsix switch is provided, then the tool validates the NuGet vsix. Else the tool validates an artifacts location for the NuGet code base.
  -p|--vsix-path          Path to NuGet.Tools.Vsix containing all english and translated dlls.
  -e|--vsix-extract-path  Path to extract NuGet.Tools.Vsix into. Folder need not be present, but Program should have write access to the location.
  -o|--output-path        Path to the directory for writing errors. File need not be present, but Program should have write access to the location.
  -c|--comments-path      Path to the local NuGet localization repository. e.g. - <repo_root>\Main\localize\comments\15
  -a|--artifacts-path     Path to the local NuGet artifacts folder. This option is used
```

NuGet Localization repository - https://github.com/NuGet/NuGet.Build.Localization

To validate the artifatcs directory do not pass the `-x|--vsix` switch. Further you need to pass the location of the artifacts directory using the `-a|--artifacts-path` option.

* `git clone https://github.com/mishra14/NuGetBuildValidators.git`
* `cd NuGetBuildValidators`
* `cd NuGetValidator.Localization`
* `msbuild /t:Restore`
* `msbuild`
* `.\NuGetValidator\bin\Debug\net461\NuGetValidator.exe localization --artifacts-path "Path\to\Nuget.Client_repo\Artifacts" --output-path "Path\to\log\" --comments-path <NuGet_Localization_repository>\Main\localize\comments\15"`


NuGet Localization repository - https://github.com/NuGet/NuGet.Build.Localization


### Using NuGetValidator.exe to validate Nuget.Tools.Vsix

The NuGetValidator.exe accepts the following arguments - 

```
PS> NuGetValidator.exe localization -h
nugetvalidator Version: 1.0.0.0


Usage: nugetvalidator localization [options]

Options:
  -h|--help               Show help information
  -x|--vsix               Switch to indicate that a vsix needs to be validate. If -x|--vsix switch is provided, then the tool validates the NuGet vsix. Else the tool validates an artifacts location for the NuGet code base.
  -p|--vsix-path          Path to NuGet.Tools.Vsix containing all english and translated dlls.
  -e|--vsix-extract-path  Path to extract NuGet.Tools.Vsix into. Folder need not be present, but Program should have write access to the location.
  -o|--output-path        Path to the directory for writing errors. File need not be present, but Program should have write access to the location.
  -c|--comments-path      Path to the local NuGet localization repository. e.g. - <repo_root>\Main\localize\comments\15
  -a|--artifacts-path     Path to the local NuGet artifacts folder. This option is used
```

To validate the artifatcs directory pass the `-x|--vsix` switch. Further you need to pass the location of the vsix and the path to extract the vsix using the `-p|--vsix-path` and `-e|--vsix-extract-path` options respectively.

NuGet Localization repository - https://github.com/NuGet/NuGet.Build.Localization

* `git clone https://github.com/mishra14/NuGetBuildValidators.git`
* `cd NuGetBuildValidators`
* `cd NuGetValidator.Localization`
* `msbuild /t:Restore`
* `msbuild`
* `.\NuGetValidator\bin\Debug\net461\NuGetValidator.exe --vsix-path "Path\to\vsix\NuGet.Tools.vsix"  --vsix-extract-path "Path\to\extract\NuGet.Tools.Vsix" --output-path "Path\to\log\" --comments-path <NuGet_Localization_repository>\Main\localize\comments\15"`

### Using NuGet Package NuGetValidator.Localization.nupkg

Things are in flight, these instructions will be added soon....

## Output

Output summary is displayed on the console. The tool generates multiple logs indicating different types of failures. The summary on the console displays the type of failure and the corresponding log file.
