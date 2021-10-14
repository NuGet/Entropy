[CmdletBinding()]
param (
    [Parameter(Mandatory=$True)]
    [string]$Version
)

dotnet build /p:NuGetMajorMinorVersion="6.0"
dotnet run