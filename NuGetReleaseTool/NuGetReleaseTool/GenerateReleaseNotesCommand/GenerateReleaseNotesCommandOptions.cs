using CommandLine;
using CommandLine.Text;

namespace NuGetReleaseTool.GenerateReleaseNotesCommand
{
    [Verb("generate-release-notes", HelpText = "Generates the release notes for the NuGet Client for a given release version.")]
    public class GenerateReleaseNotesCommandOptions : BaseOptions
    {
        [Value(0, Required = true, HelpText = "Release version to generate the release notes for.")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Release { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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
                    new Example("Generate release notes for a particular release", new GenerateReleaseNotesCommandOptions { Release = "6.3", GitHubToken = "asdf", EndCommit = "endSha" })
                };
            }
        }
    }
}
