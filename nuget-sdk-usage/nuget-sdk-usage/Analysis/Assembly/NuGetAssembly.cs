using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace nuget_sdk_usage.Analysis.Assembly
{
    internal static class NuGetAssembly
    {
        internal static IReadOnlyList<string> NuGetAssemblyNames = new List<string>()
        {
            "Microsoft.Build.NuGetSdkResolver",
            "NuGet",
            "NuGet.Build.Tasks",
            "NuGet.Build.Tasks.Console",
            "NuGet.Build.Tasks.Pack",
            "NuGet.CommandLine.XPlat",
            "NuGet.Commands",
            "NuGet.Common",
            "NuGet.Configuration",
            "NuGet.Console",
            "NuGet.Credentials",
            "NuGet.DependencyResolver.Core",
            "NuGet.Frameworks",
            "NuGet.Indexing",
            "NuGet.LibraryModel",
            "NuGet.Localization",
            "NuGet.MSSigning.Extensions",
            "NuGet.PackageManagement",
            "NuGet.PackageManagement.PowerShellCmdlets",
            "NuGet.PackageManagement.UI",
            "NuGet.PackageManagement.VisualStudio",
            "NuGet.Packaging",
            "NuGet.Packaging.Core",
            "NuGet.ProjectModel",
            "NuGet.Protocol",
            "NuGet.Resolver",
            "NuGet.SolutionRestoreManager",
            "NuGet.SolutionRestoreManager.Interop",
            "NuGet.StaFact",
            "NuGet.Tools",
            "NuGet.Versioning",
            "NuGet.VisualStudio",
            "NuGet.VisualStudio.Client",
            "NuGet.VisualStudio.Common",
            "NuGet.VisualStudio.Implementation",
            "NuGet.VisualStudio.Interop",
            "NuGetConsole.Host.PowerShell"
        };

        internal static bool MatchesName(string filename)
        {
            const string resources = ".resources";
            if (filename.EndsWith(resources))
            {
                filename = filename.Substring(0, filename.Length - resources.Length);
            }

            return NuGetAssemblyNames.Contains(filename, StringComparer.OrdinalIgnoreCase);
        }

        internal static bool IsNuGetAssembly(AssemblyReference assemblyReference)
        {
            var assemblyName = assemblyReference.GetAssemblyName();

            var isNuGetAssembly = assemblyName.Name == null || NuGetAssembly.MatchesName(assemblyName.Name);

            // Originally I wanted to also check the strong name token, to reduce the risk that someone compiled their
            // code against a custom NuGet assembly with APIs that we don't ship. However, JetBrains compiled their
            // products against custom NuGet assemblies with a different public token, so we can't do that check if we
            // want to find out which APIs they're using.

            return isNuGetAssembly;
        }
    }
}
