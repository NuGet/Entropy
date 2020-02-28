# nuget-sdk-usage

This is a tool to scan compiled binaries and build a list of all NuGet SDK APIs used. If run on numerous applications
that use the NuGet SDK, it can help the NuGet client team understand which APIs are really used, and which are just
internal, even if the C# accessibility modifier is public.

Additionally, it checks which Target Framework Monikers (TFMs) assemblies that reference the NuGet SDK are using, as
well as which versions of the NuGet SDK assemblies they were compiled against.

## Usage

The application takes one parameter, the directory where to scan for assemblies. If not provided, it searches from the current directory.
The output is JSON, on standard output, which can be redirected to a file.

```ps1
nuget-sdk-usage "c:\path\to\scan\" > results.json
```

## Sample output

```json
{
  "TargetFrameworks": [
    ".NET Framework 4.6.1",
    ".NET Framework 4.5.1",
    ".NET Framework 4.7.2"
  ],
  "Versions": [
    "4.8.0.6",
    "5.0.0.6",
    "5.0.0.2",
    "4.8.0.5"
  ],
  "MemberReferences": {
    "NuGet.Versioning": [
      "NuGet.Versioning.SemanticVersion.op_Equality(NuGet.Versioning.SemanticVersion, NuGet.Versioning.SemanticVersion)",
      "NuGet.Versioning.VersionRangeBase.Satisfies(NuGet.Versioning.NuGetVersion)",
      "NuGet.Versioning.VersionRangeBase.get_HasLowerBound()",
      "NuGet.Versioning.VersionRangeBase.get_IsMinInclusive()",
      "NuGet.Versioning.VersionRangeBase.get_MinVersion()",
      "NuGet.Versioning.SemanticVersion.op_LessThan(NuGet.Versioning.SemanticVersion, NuGet.Versioning.SemanticVersion)",
      "NuGet.Versioning.SemanticVersion.op_LessThanOrEqual(NuGet.Versioning.SemanticVersion, NuGet.Versioning.SemanticVersion)",
      "NuGet.Versioning.VersionRangeBase.get_HasUpperBound()",
      "NuGet.Versioning.VersionRangeBase.get_IsMaxInclusive()",
      "NuGet.Versioning.VersionRangeBase.get_MaxVersion()",
      "NuGet.Versioning.SemanticVersion.op_GreaterThan(NuGet.Versioning.SemanticVersion, NuGet.Versioning.SemanticVersion)",
      "NuGet.Versioning.SemanticVersion.op_GreaterThanOrEqual(NuGet.Versioning.SemanticVersion, NuGet.Versioning.SemanticVersion)",
      "NuGet.Versioning.NuGetVersion.TryParse(System.String, NuGet.Versioning.NuGetVersion)",
      "NuGet.Versioning.VersionRange.Parse(System.String)",
      "NuGet.Versioning.SemanticVersion.ToFullString()",
      "NuGet.Versioning.VersionRange.CommonSubSet(System.Collections.Generic.IEnumerable\u00601\u003CNuGet.Versioning.VersionRange\u003E)",
      "NuGet.Versioning.VersionRange.TryParse(System.String, NuGet.Versioning.VersionRange)",
      "NuGet.Versioning.SemanticVersion.ToNormalizedString()",
      "NuGet.Versioning.NuGetVersion.Parse(System.String)"
    ],
    "NuGet.Frameworks": [
      "NuGet.Frameworks.FrameworkConstants.CommonFrameworks.Net35",
      "NuGet.Frameworks.FrameworkConstants.CommonFrameworks.Net4",
      "NuGet.Frameworks.FrameworkConstants.CommonFrameworks.Net45",
      "NuGet.Frameworks.FrameworkConstants.CommonFrameworks.NetCoreApp10",
      "NuGet.Frameworks.FrameworkConstants.CommonFrameworks.UAP10",
      "NuGet.Frameworks.NuGetFramework.Parse(System.String)",
      "NuGet.Frameworks.NuGetFramework.get_IsUnsupported()",
      "NuGet.Frameworks.NuGetFramework.get_DotNetFrameworkName()",
      "NuGet.Frameworks.NuGetFramework.get_Version()",
      "NuGet.Frameworks.NuGetFramework.GetShortFolderName()"
    ],
    "NuGet.ProjectModel": [
      "NuGet.ProjectModel.LockFileUtilities.GetLockFile(System.String, NuGet.Common.ILogger)",
      "NuGet.ProjectModel.LockFile.get_Targets()",
      "NuGet.ProjectModel.LockFileTarget.get_Libraries()",
      "NuGet.ProjectModel.LockFileTargetLibrary.get_Type()",
      "NuGet.ProjectModel.LockFileTargetLibrary.get_CompileTimeAssemblies()",
      "NuGet.ProjectModel.LockFileItem.get_Path()",
      "NuGet.ProjectModel.LockFileTargetLibrary.get_Name()",
      "NuGet.ProjectModel.LockFileTargetLibrary.get_Version()"
    ],
    "NuGet.Protocol": [
      "NuGet.Protocol.Plugins.IRequestHandlers.TryAdd(NuGet.Protocol.Plugins.MessageMethod, NuGet.Protocol.Plugins.IRequestHandler)",
      "NuGet.Protocol.Plugins.AutomaticProgressReporter.Create(NuGet.Protocol.Plugins.IConnection, NuGet.Protocol.Plugins.Message, System.TimeSpan, System.Threading.CancellationToken)",
      "NuGet.Protocol.Plugins.GetAuthenticationCredentialsRequest.get_IsRetry()",
      "NuGet.Protocol.Plugins.GetAuthenticationCredentialsRequest.get_Uri()",
      "NuGet.Protocol.Plugins.GetOperationClaimsRequest.get_PackageSourceRepository()",
      "NuGet.Protocol.Plugins.GetOperationClaimsRequest.get_ServiceIndex()",
      "NuGet.Protocol.Plugins.GetOperationClaimsResponse..ctor(System.Collections.Generic.IEnumerable\u00601\u003CNuGet.Protocol.Plugins.OperationClaim\u003E)",
      "NuGet.Protocol.Plugins.InitializeResponse..ctor(NuGet.Protocol.Plugins.MessageResponseCode)",
      "NuGet.Protocol.Plugins.Message.get_Method()",
      "NuGet.Protocol.Plugins.Message.get_RequestId()",
      "NuGet.Protocol.Plugins.SetCredentialsResponse..ctor(NuGet.Protocol.Plugins.MessageResponseCode)",
      "NuGet.Protocol.Plugins.SetLogLevelRequest.get_LogLevel()",
      "NuGet.Protocol.Plugins.SetLogLevelResponse..ctor(NuGet.Protocol.Plugins.MessageResponseCode)",
      "NuGet.Protocol.Plugins.LogRequest..ctor(NuGet.Common.LogLevel, System.String)",
      "NuGet.Protocol.Plugins.IConnection.SendRequestAndReceiveResponseAsync(NuGet.Protocol.Plugins.MessageMethod, T0, System.Threading.CancellationToken)",
      "NuGet.Protocol.Plugins.GetAuthenticationCredentialsResponse..ctor(System.String, System.String, System.String, System.Collections.Generic.IList\u00601\u003CSystem.String\u003E, NuGet.Protocol.Plugins.MessageResponseCode)",
      "NuGet.Protocol.Plugins.ConnectionOptions.CreateDefault(NuGet.Common.IEnvironmentVariableReader)",
      "NuGet.Protocol.Plugins.PluginFactory.CreateFromCurrentProcessAsync(NuGet.Protocol.Plugins.IRequestHandlers, NuGet.Protocol.Plugins.ConnectionOptions, System.Threading.CancellationToken)",
      "NuGet.Protocol.Plugins.IPlugin.get_Connection()",
      "NuGet.Protocol.Plugins.IRequestHandlers.TryGet(NuGet.Protocol.Plugins.MessageMethod, NuGet.Protocol.Plugins.IRequestHandler)",
      "NuGet.Protocol.Plugins.GetAuthenticationCredentialsRequest..ctor(System.Uri, System.Boolean, System.Boolean, System.Boolean)",
      "NuGet.Protocol.Plugins.GetAuthenticationCredentialsResponse.get_Username()",
      "NuGet.Protocol.Plugins.GetAuthenticationCredentialsResponse.get_Password()",
      "NuGet.Protocol.Plugins.ProtocolErrorEventArgs.get_Message()",
      "NuGet.Protocol.Plugins.Message.get_Type()",
      "NuGet.Protocol.Plugins.ProtocolErrorEventArgs.get_Exception()",
      "NuGet.Protocol.Plugins.IConnection.add_Faulted(System.EventHandler\u00601\u003CNuGet.Protocol.Plugins.ProtocolErrorEventArgs\u003E)",
      "NuGet.Protocol.Plugins.IPlugin.add_BeforeClose(System.EventHandler)",
      "NuGet.Protocol.Plugins.IPlugin.add_Closed(System.EventHandler)",
      "NuGet.Protocol.Plugins.GetAuthenticationCredentialsRequest.get_IsNonInteractive()",
      "NuGet.Protocol.Plugins.GetAuthenticationCredentialsRequest.get_CanShowDialog()",
      "NuGet.Protocol.Plugins.GetAuthenticationCredentialsResponse.get_ResponseCode()",
      "NuGet.Protocol.Plugins.GetAuthenticationCredentialsResponse.get_Message()",
      "NuGet.Protocol.Plugins.MessageUtilities.DeserializePayload(NuGet.Protocol.Plugins.Message)",
      "NuGet.Protocol.Plugins.Message.get_Payload()",
      "NuGet.Protocol.Plugins.MessageUtilities.Create(System.String, NuGet.Protocol.Plugins.MessageType, NuGet.Protocol.Plugins.MessageMethod)",
      "NuGet.Protocol.Plugins.IConnection.SendAsync(NuGet.Protocol.Plugins.Message, System.Threading.CancellationToken)",
      "NuGet.Protocol.Plugins.IResponseHandler.SendResponseAsync(NuGet.Protocol.Plugins.Message, T0, System.Threading.CancellationToken)"
    ]
  }
}
```
