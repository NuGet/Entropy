# generate-release-notes

A command of the release tool to generate the release notes for a given release.

```console
NuGetReleaseTool.exe generate-release-notes 6.4 
```

The release endpoint is resolved automatically from the release branch tip, falling back to a matching release tag if the branch no longer exists.

## To build and run the tool

- Go to the NuGetRelease notes project folder
- Run `dotnet run generate-release-notes <MajorVersion.MinorVersion>`
- After running the command, there should be an md file in the directory