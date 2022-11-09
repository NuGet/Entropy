using CommandLine.Text;
using CommandLine;

namespace NuGetReleaseTool.ValidateReleaseCommand
{
    [Verb("validate-release", HelpText = "Validates a specific NuGet release. Checks things such as NuGet.exe, NuGet SDK packages, release notes, open docs issues etc. " +
        "The tool automatically generates a diff between this and the previous version by looking up at the tips of both branches.")]
    public class ValidateReleaseCommandOptions : BaseOptions
    {
        [Value(1, Required = true, HelpText = "Release version to validate the release for.")]
        public string Release { get; set; }

        [Option("end-commit", Required = false, HelpText = "The end commit for the current release. This commit must be on the release branch. " +
            "You do not normally need to use this argument, unless there's a commit on the branch, that is not within the current release.")]
        public string? EndCommit { get; set; }

        [Usage(ApplicationAlias = "validate-release")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example("Generate a validation report for a particular release.", new ValidateReleaseCommandOptions { Release = "6.3" })
                };
            }
        }
    }
}
