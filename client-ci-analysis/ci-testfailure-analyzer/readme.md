# ci-testfailure-analyzer

Steps to use:

1. Go to AzDo, view build, view and download build logs for run test phase
2. Run this app, passing log directory path.
  * It'll make AzDo api calls to discover failed test within last 30 days and summarize failed test data.