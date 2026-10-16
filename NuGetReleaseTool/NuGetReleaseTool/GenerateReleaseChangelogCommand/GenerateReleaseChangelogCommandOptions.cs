using CommandLine.Text;
using CommandLine;

namespace NuGetReleaseTool.GenerateReleaseChangelogCommand
{
    [Verb("generate-release-changelog", HelpText = "Generate an insertion changelog.")]
    public class GenerateReleaseChangelogCommandOptions : BaseOptions
    {
        [Value(0, Required = true, HelpText = "Release version to generate the release notes for.")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Release { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Usage(ApplicationAlias = "generate-release-changelog")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example("Generate changelog for a particular release", new GenerateReleaseChangelogCommandOptions { Release = "6.3", GitHubToken = "asdf" })
                };
            }
        }

        [Option("output", Required = false, HelpText = "Directory to output the results file in.")]
        public string? Output { get; set; }
    }
}
