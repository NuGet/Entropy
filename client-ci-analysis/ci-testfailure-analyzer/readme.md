# ci-testfailure-analyzer

Tools to help analyze CI builds for failed/flaky tests from last 30 days then it generates cvs report into output directory specified in parameter.

## Setup following env vars for authentication

1. `AzDO_PAT`: PAT token. Instead you can put PAT token in `pat.txt` of output directory. Please select `Build -> Read`, `Test Management -> Read, Write`, `Package -> Read` scopes for PAT.
1. `AzDO_ACCOUNT`: optional env var. Use this to override account used for PAT token, by default it's current user.

## Synopsis

```dotnetcli
ci-testfailure-analyzer.exe <OUTPUT_DIRECTORY_PATH> <CI_PIPELINE_ID>
```

## Arguments

- **`OUTPUT_DIRECTORY_PATH`**

  Path for where cvs report generated into.

- **`CI_PIPELINE_ID`**

  Integer id number of the CI pipeline you wish to analyze. By default it targets NuGet official CI pipeline for creating report.
