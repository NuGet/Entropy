using EnvDTE;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace IVsTestingExtension.Tests
{
    [Export(typeof(ITestMethodProvider))]
    public class TestMethodProvider : ITestMethodProvider
    {
        [Import]
        IVsPackageInstaller VsAsyncPackageInstaller { get; set; }

        public Func<Project, Dictionary<string, string>, Task> GetMethod() => TestSyncInstallPackage;

        private async Task TestSyncInstallPackage(Project projectSelected, Dictionary<string, string> arguments)
        {
            arguments.TryGetValue("packageId", out string packageId);
            arguments.TryGetValue("packageVersion", out string packageVersion);
            arguments.TryGetValue("source", out string source);
            arguments.TryGetValue("ignoreDependencies", out string ignoreDependenciesStr);
            bool.TryParse(ignoreDependenciesStr, out bool ignoreDependencies);

            VsAsyncPackageInstaller.InstallPackage(source: source,
                                              projectSelected,
                                              packageId,
                                              packageVersion,
                                              ignoreDependencies: ignoreDependencies);
        }
    }
}
