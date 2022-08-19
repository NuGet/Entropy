# ci-testfailure-analyzer

Tools to help analyze CI builds for failed/flaky tests, it generates cvs report into output directory specified in parameter.

## Setup following env vars for authentication

1. "AzDO_ACCOUNT" : your MS user id.
1. "AzDO_BEARERTOKEN": Bearer token. Since bearer token(oauth2) expire often you can instead set in `bearerToken.txt` file (located in output directory) and refresh it when it expires. Please note don't include `Bearer` part of bearer authorization token.

## Synopsis

```dotnetcli
ci-testfailure-analyzer.exe <OUTPUT_DIRECTORY_PATH> <CI_PIPELINE_ID>
```

## Arguments

- **`OUTPUT_DIRECTORY_PATH`**

  Path to the cvs report generated to. Also

- **`CI_PIPELINE_ID`**

  Integer id number of the CI pipeline you wish to analyze.
