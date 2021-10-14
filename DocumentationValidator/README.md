# Documentation Validator

This is a helper tool to validate that all of our log codes have appropriate documentation published.

```console
dotnet build /p:NuGetMajorMinorVersion="6.0"
dotnet run
```

or 

```powershell
.\run.ps1 -Version 6.0
```

The exit code will be `1` if any codes are found to be undocumented, and `0` if all the codes are documented.
