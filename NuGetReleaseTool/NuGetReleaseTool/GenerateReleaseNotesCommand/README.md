# generate-release-notes

A command of the release tool to generate the release notes for a given release.

```console
NuGetReleaseTool.exe generate-release-notes 6.4 
```

This tool also has an `--end-commit` option, `--end-commit 99e6dc73e6d23fce650327a21a5670a7039585db`. 
This would indicate to the tool that not all commits on release branch for the release are part of the release. Normally, you would not use this option.

## To build and run the tool

- Go to the NuGetRelease notes project folder
- If the release branch does not have any commits that are going to be in the release run the following:
  - dotnet run generate-release-notes <MajorVersion.MinorVersion> 
- If the release branch has any commits that are not going to be in the release run the following:
  - dotnet run generate-release-notes <MajorVersion.MinorVersion> --end-commit <end-commit-sha>
- After running the command, there should be an md file in the directory