# Release Notes Generator tool

- Go to ReleaseNotesGenerator project directory
- If the release branch has any commits that are not going to be in the release run the following:
  - dotnet run <MajorVersion.MinorVersion> --start-commit startCommitSha
- If the release branch does not have any commits that are going to be in the release run the following:
  - dotnet run <MajorVersion.MinorVersion> --start-commit startCommitSha --end-commit
- After running the command, there should be an md file in the directory