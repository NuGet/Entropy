[CmdletBinding()]
param (
    [Parameter]
    [string]$Version
)

dotnet build /p:NuGetMajorMinorVersion=$Version
dotnet run get-undocumented-codes