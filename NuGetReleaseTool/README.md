# NuGet Release Tool

This is a tool that's contains helper commands to ship releases, create insertions, and other helpers that help deliver NuGet to our customers. 

To run the tool:

- Build the project and run the `NuGetReleaseTool.exe help`
- Go to the project folder:
  - cd `NuGetReleaseTool`
  - dotnet run help


## Command List

This is a manually managed list of commands available in the tool. It is expected that each command has a README of their own and has a its own folder.

- [generate-release-notes](.\NugetReleaseTool\GenerateReleaseNotesCommand\README.md)
- [generate-insertion-changelog](.\NugetReleaseTool\GenerateInsertionChangelogCommand\README.md)
- [validate-release](.\NugetReleaseTool\ValidateReleaseCommand\README.md)
