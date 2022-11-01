# validate-release

This is a command to help validate that all items of a NuGet release are publicly available. 


```console
NuGetReleaseTool.exe validate-release 6.4
```

## Sample output

Here's a sample output for 6.4 as of 11/1/22.

|Section | Status | Notes |
|--------|--------|-------|
| Release notes | InProgress | The docs repo has a PR for the release notes. https://github.com/NuGet/docs.microsoft.com-nuget/pull/2926 |
| Documentation readiness | NotStarted | Documentation readiness not being evaluated yet. |
| SDK packages | NotStarted | NuGet.Indexing, NuGet.Build.Tasks.Console, NuGet.Build.Tasks.Pack, NuGet.Build.Tasks, NuGet.CommandLine.XPlat, NuGet.Commands, NuGet.Common, NuGet.Configuration, NuGet.Credentials, NuGet.DependencyResolver.Core, NuGet.Frameworks, NuGet.LibraryModel, NuGet.Localization, NuGet.PackageManagement, NuGet.Packaging.Core, NuGet.Packaging, NuGet.ProjectModel, NuGet.Protocol, NuGet.Resolver, NuGet.Versioning are not uploaded. |
| NuGet.exe | NotStarted | Not started |

Here's a sample output for 6.3 as of 11/1/22.

|Section | Status | Notes |
|--------|--------|-------|
| Release notes | Completed | https://learn.microsoft.com/en-us/nuget/release-notes/nuget-6.3 |
| Documentation readiness | NotStarted | Documentation readiness not being evaluated yet. |
| SDK packages | Completed |  |
| NuGet.exe | Completed | 6.3 is on NuGet.org, and considered blessed |