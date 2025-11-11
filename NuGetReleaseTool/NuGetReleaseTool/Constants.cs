namespace NuGetReleaseTool
{
    public static class Constants
    {
        public const string NuGet = "NuGet";
        public const string NuGetClient = "nuget.client";
        public const string Home = "home";
        public const string DocsRepo = "docs.microsoft.com-nuget";
        public const string NuGetCommandlinePackageId = "NuGet.CommandLine";

        // Core packages list. This excludes NuGet.Commandline.
        public static List<string> CorePackagesList = new List<string>() {
            "NuGet.Indexing",
            "NuGet.Commands",
            "NuGet.Common",
            "NuGet.Configuration",
            "NuGet.Credentials",
            "NuGet.DependencyResolver.Core",
            "NuGet.Frameworks",
            "NuGet.LibraryModel",
            "NuGet.Localization",
            "NuGet.PackageManagement",
            "NuGet.Packaging",
            "NuGet.ProjectModel",
            "NuGet.Protocol",
            "NuGet.Resolver",
            "NuGet.Versioning" };

        // VS Packages List. This includes packages that version the same way as VS.
        public static List<string> VSPackagesList = new()
        {
            "NuGet.VisualStudio.Contracts",
            "NuGet.VisualStudio",
        };
    }
}
