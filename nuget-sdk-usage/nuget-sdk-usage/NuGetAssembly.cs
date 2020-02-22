using System;
using System.Collections.Generic;
using System.Linq;

namespace nuget_sdk_usage
{
    internal static class NuGetAssembly
    {
        public static IReadOnlyList<string> NuGetAssemblyNames = new List<string>()
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

        public static IReadOnlyList<byte[]> NuGetStrongNamePublicKeyTokens = ConvertPublicKeyTokens(
            "31bf3856ad364e35", // NuGet.Core assemblies
            "b03f5f7f11d50a3a" // NuGet.Client assemblies
            );

        public static bool MatchesName(string filename)
        {
            const string resources = ".resources";
            if (filename.EndsWith(resources))
            {
                filename = filename.Substring(0, filename.Length - resources.Length);
            }

            return NuGetAssemblyNames.Contains(filename, StringComparer.OrdinalIgnoreCase);
        }

        internal static bool IsMicrosoftPublicToken(byte[] token)
        {
            if (token == null)
            {
                return false;
            }

            foreach (var nugetToken in NuGetStrongNamePublicKeyTokens)
            {
                if (nugetToken.Length == token.Length)
                {
                    bool matches = true;
                    for (int i = 0; i < token.Length; i++)
                    {
                        if (token[i] != nugetToken[i])
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (matches)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static IReadOnlyList<byte[]> ConvertPublicKeyTokens(params string[] tokens)
        {
            var result = new List<byte[]>(tokens.Length);

            foreach (var token in tokens)
            {
                var bytes = new byte[token.Length / 2];
                for (int position = 0; position < bytes.Length; position++)
                {
                    bytes[position] = Convert.ToByte(token.Substring(position * 2, 2), 16);
                }

                result.Add(bytes);
            }

            return result;
        }
    }
}
