# add-milestone

A command of the release tool to add milestones to issues that don't have one or don't have the expected one.
It is recommended that you always do a dry run first.

Sample commandline arguments:

```console
NuGetReleaseTool.exe add-milestone 6.7 --dry-run --correct-milestones
```

### GitHub Personal Access Token

The tool will use [`git credential`](https://git-scm.com/docs/git-credential) to try to get a personal access token for GitHub. Unlike `git push`, it will not prompt you if it cannot find a PAT or if the PAT is expired. To have this tool work automatically again, you could use `git push`, which will use the interactive credential manager to update the saved token.

This tool also supports `--github-token <string>` as an argument to use a specific PAT.

If you do not provide `--github-token`, and it cannot be obtained automatically from `git credential`, this tool with make unauthenticated HTTP requests to GitHub. GitHub provide a significantly lower HTTP rate limit per hour for anonymous requests, so if there are a large number of commits, this tool might fail. For more information, see https://docs.github.com/en/rest/overview/resources-in-the-rest-api#rate-limiting.
