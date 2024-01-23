# SDK Patch Tool How To

1.Go to the SDKPatchTool folder
2.Run .\SDKPatch.ps1 -SDKPath <sdk path> -NupkgsPath <nupks path>
4.If you see "Finish patching", you may start testing the patched dotnet now, with absolute path
  If you see "Patching failed", you have to check why the patching is failed.

You may choose to specify the SDK Channel as well.
To know what the appropriate channel is you can refer to NuGet/SDK mapping in the NuGet the release notes: <https://learn.microsoft.com/en-us/nuget/release-notes/nuget-6.8>

- Each November release, the channel needs to be increment. For November 2023, the best channel is 8.0.
