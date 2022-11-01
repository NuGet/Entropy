using CommandLine.Text;
using CommandLine;

namespace NuGetReleaseTool.ValidateReleaseCommand
{
    [Verb("validate-release", HelpText = "Validates a specific NuGet release. Checks things such as NuGet.exe, NuGet SDK packages, release notes, open docs issues etc.")]
    public class ValidateReleaseCommandOptions : BaseOptions
    {
        [Value(1, Required = true, HelpText = "Release version to generate the release notes for.")]
        public string Release { get; set; }

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
