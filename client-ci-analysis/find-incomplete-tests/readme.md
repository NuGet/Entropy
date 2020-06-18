# find-incomplete-tests

Steps to use:

1. Go to AzDo, view build, view and download build logs for run test phase
2. Run this app, passing all log files as arguments
  * easy way is to use bash and use `dotnet run *.txt`. Note that PowerShell doesn't do file globbing, but bash does.
