# Insertion Change Log Generator

A helper tool to generate a list of commits for NuGet Client insertions.

This project is a global tool.

Sample commandline arguments:

```console
InsertionChangeLogGenerator.exe generate --startSha sha123456789 --branch dev --output D:\Output
```

### GitHub Personal Access Token

The tool will use [`git credential`](https://git-scm.com/docs/git-credential) to try to get a personal access token for GitHub. Unlike `git push`, it will not prompt you if it cannot find a PAT or if the PAT is expired. To have this tool work automatically again, you could use `git push`, which will use the interactive credential manager to update the saved token.

This tool also supports `--pat <string>` as an argument to use a specific PAT.

If you do not provide `--pat`, and it cannot be obtained automatically from `git credential`, this tool with make unauthenticated HTTP requests to GitHub. GitHub provide a significantly lower HTTP rate limit per hour for anonymous requests, so if there are a large number of commits, this tool might fail. For more information, see https://docs.github.com/en/rest/overview/resources-in-the-rest-api#rate-limiting.
