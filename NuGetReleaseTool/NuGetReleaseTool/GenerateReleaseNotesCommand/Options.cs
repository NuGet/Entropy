using CommandLine;
using CommandLine.Text;

namespace GenerateReleaseNotesCommand
{
    [Verb("generate-release-notes", HelpText = "Generates the release notes for the NuGet Client for a given release version.")]
    public class GenerateReleaseNotesCommandOptions
    {
        [Value(0, Required = true, HelpText = "Release version to generate the release notes for.")]
        public string Release { get; set; }

        [Option('g', "github-token", Required = false, HelpText = "GitHub Token for Auth. If not specified, it will acquired automatically.")]
        public string GitHubToken { get; set; }

        [Option("start-commit", Required = true, HelpText = "The starting sha for the current release. This commit must be on the release branch.")]
        public string StartSha { get; set; }

        [Option("end-commit", Required = false, HelpText = "The starting sha for the current release. This commit must be on the release branch. If not specified, the tip of the release branch will be used.")]
        public string EndSha { get; set; }

        [Usage(ApplicationAlias = "release-notes-generator")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example("Generate release notes for a particular release", new GenerateReleaseNotesCommandOptions { Release = "6.3", GitHubToken = "asdf", StartSha =" startSha", EndSha = "endSha" })
                };
            }
        }
    }
}
