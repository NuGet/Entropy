# NuGetBuildValidators.Localization

## Introduction

This is a tool used to validate the localized strings for NuGet.Tools.vsix.

## Build Package

To build `NuGetValidator.Localization.2.0.0.nupkg` - 

1. `cd NuGetBuildValidators`
2. `msbuild NuGetValidators.sln /t:"restore;build" /p:configuration=release /p:pack=true`
3. Artifact is available at `NuGetBuildValidators\NuGetValidator.Localization\bin\Release\NuGetValidator.Localization.2.0.0.nupkg`

## Usage

### Using NuGet Package NuGetValidator.Localization.nupkg

Add a package reference - 

```
  <ItemGroup>    
    <PackageReference Include="NuGetValidator.Localization" Version="2.0.0" />    
  </ItemGroup>
```
