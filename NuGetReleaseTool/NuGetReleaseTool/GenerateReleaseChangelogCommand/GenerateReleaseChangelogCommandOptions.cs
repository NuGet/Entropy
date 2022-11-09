using CommandLine.Text;
using CommandLine;
using NuGetReleaseTool.GenerateReleaseNotesCommand;

namespace NuGetReleaseTool.GenerateReleaseChangelogCommand
{
    [Verb("generate-release-changelog", HelpText = "Generate an insertion changelog.")]
    public class GenerateReleaseChangelogCommandOptions : BaseOptions
    {
        [Value(0, Required = true, HelpText = "Release version to generate the release notes for.")]
        public string Release { get; set; }

        [Option("end-commit", Required = false, HelpText = "The end commit for the current release. This commit must be on the release branch. " +
            "You do not normally need to use this argument, unless there's a commit on the branch, that is not within the current release.")]
        public string? EndCommit { get; set; }

        [Usage(ApplicationAlias = "generate-release-notes")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example("Generate changelog for a particular release", new GenerateReleaseNotesCommandOptions { Release = "6.3", GitHubToken = "asdf", EndCommit = "endSha" })
                };
            }
        }

        [Option("output", Required = false, HelpText = "Directory to output the results file in.")]
        public string? Output { get; set; }
    }
}
