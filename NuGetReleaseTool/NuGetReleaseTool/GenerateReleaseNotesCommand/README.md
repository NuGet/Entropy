# generate-release-notes

A command of the release tool to generate the release notes for a given release.

```console
NuGetReleaseTool.exe generate-release-notes 6.4 --start-commit sha123456789 
```

## To build and run the tool

- Go to the NuGetRelease notes project folder
- If the release branch has any commits that are not going to be in the release run the following:
  - dotnet run generate-release-notes <MajorVersion.MinorVersion> --start-commit startCommitSha
- If the release branch does not have any commits that are going to be in the release run the following:
  - dotnet run generate-release-notes <MajorVersion.MinorVersion> --start-commit startCommitSha --end-commit
- After running the command, there should be an md file in the directory