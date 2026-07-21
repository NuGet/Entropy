# NuGetClientPRHealth

Generates an HTML dashboard showing PR review health for the NuGet.Client team over a configurable time window.

## Usage

```
dotnet run [--token=<PAT>] [--days=<N>] [--output=<file.html>]
```

## Inputs

| Argument | Default | Description |
|---|---|---|
| `--token=<PAT>` | `GITHUB_TOKEN` env var | GitHub personal access token (no scopes needed). Without it, rate limit is 60 req/hr. |
| `--days=<N>` | `14` | Number of past days to include. |
| `--output=<file>` | `nuget-pr-health-<date>.html` | Output file path. |

## Output

A self-contained `.html` file with:
- Summary metrics: total PRs, median hours to merge, % approved/merged under 24 h
- Table of slow PRs (>72 h to merge)
- Full table of all merged PRs in the window

## Notes

- Only PRs authored by known team members are included (hardcoded list in `DashboardService.cs`).
- Requires ~2 GitHub API calls per PR (timeline + reviews). The tool checks your remaining rate limit before proceeding.
