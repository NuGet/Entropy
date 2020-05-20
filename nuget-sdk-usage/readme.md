# nuget-sdk-usage

This is a tool to help the NuGet client team better understand what NuGet SDK APIs are actually used, and which APIs have
lower risk of causing problems if breaking changes are implemented.

## Usage

The application has two modes, `scan` and `update`.

### Scan

To scan a directory for compiled assemblies, and analyse those assemblies for references to NuGet SDK assemblies, use the `scan` command.

The command takes one argument, the directory to scan. It recursively searches all subdirectories for `*.dll` and `*.exe`, then analyses them to determine if they're .NET assemblies, and if so, if they reference NuGet SDK assemblies.

There is an optional argument `--output`, which can be used to specify the filename of where the results should be written. If this argument is omitted, the scan results will be output to the console. Note that Windows shells often write text files with a Byte Order Marker (BOM), but this tool uses `System.Text.Json`, which does not support BOM, so it is generally does not work on Windows to redirect `scan` results to a file.

```ps1
nuget-sdk-usage "c:\path\to\scan\\" --output results.json
```

### Update

The `update` command will merge all `scan` results into a list of all APIs known to be used, then modify the `NuGet.Client` repo to mark those APIs with a `UsedNuGetSdkApi` attribute. It has two arguments.

`--results` is used to specify the directory where `scan` results are. It will load all `*.json` files.

`--source` specifies the location of the `NuGet.Client` repo.

```ps1
nuget-sdk-usage --results "c:\path\to\results\\" --source "c:\path\to\NuGet.Client\\"
```
