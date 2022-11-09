using CommandLine;

namespace NuGetReleaseTool.GenerateInsertionChangelogCommand
{
    [Verb("unlist-redundant-packages", HelpText = "Unlist redundant packages.")]
    public class UnlistRedundantPackagesCommandOptions : BaseOptions
    {
        [Option("dry-run", Required = false, HelpText = "A dry run will print the list of packages to unlist.")]
        public bool DryRun { get; set; }

        [Option("api-key", Required = false, HelpText = "The API key to use to unlist.")]
        public string APIKey { get; set; }

    }
}
