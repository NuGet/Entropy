# generate-release-changelog

A command of the release tool to generate a list of commits for a particular release. 
The commands and options here match what the [validate-release](../ValidateReleaseCommand/README.md) and [generate-release-notes](../GenerateReleaseNotesCommand/README.md) commands do.
The commits for a release are generated in the same way as they are in the release notes generator and validate release commands.

Sample commandline arguments:

```console
NuGetReleaseTool.exe generate-release-changelog 6.4
```

This tool also has an `--end-commit` option, `--end-commit 99e6dc73e6d23fce650327a21a5670a7039585db`. 
This would indicate to the tool that not all commits on release branch for the release are part of the release. Normally, you would not use this option.