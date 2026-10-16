# generate-release-changelog

A command of the release tool to generate a list of commits for a particular release. 
The commands and options here match what the [validate-release](../ValidateReleaseCommand/README.md) and [generate-release-notes](../GenerateReleaseNotesCommand/README.md) commands do.
The commits for a release are generated in the same way as they are in the release notes generator and validate release commands.

Sample commandline arguments:

```console
NuGetReleaseTool.exe generate-release-changelog 6.4
```

The release endpoint is resolved automatically from the release branch tip, falling back to a matching release tag if the branch no longer exists.