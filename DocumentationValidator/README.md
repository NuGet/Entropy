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

You can always use the tool, `DocumentationValidator.exe` directly as well.


```console
DocumentationValidator.exe help
DocumentationValidator 1.0.0
Copyright (C) 2021 DocumentationValidator

  get-undocumented-codes    Generates a markdown table of the undocumented log codes with their relevant issues if any.

  generate-issues           Creates issues in the docs repo for the log codes that are undocumented.

  help                      Display more information on a specific command.

  version                   Display version information.
```

```console
DocumentationValidator.exe generate-issues --help
DocumentationValidator 1.0.0
Copyright (C) 2021 DocumentationValidator

  --pat        Required. A Github PAT from a user with sufficient permissions to perform the invoked action.

  --help       Display this help screen.

  --version    Display version information.
```

```console
DocumentationValidator.exe get-undocumented-codes --help
DocumentationValidator 1.0.0
Copyright (C) 2021 DocumentationValidator

  --help       Display this help screen.

  --version    Display version information.
```