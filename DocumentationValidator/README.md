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

The helper tool contains a capability to create issues, which can be turned on by passing arguments. 

```console
DocumentationValidator.exe 123456 true
```
where `123456` is a github API key and `true` indicates that issues should be created.