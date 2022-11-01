using CommandLine.Text;
using CommandLine;

namespace NuGetReleaseTool.ValidateReleaseCommand
{
    [Verb("validate-release", HelpText = "Generates the release notes for the NuGet Client for a given release version.")]
    internal class ValidateReleaseCommandOptions : BaseOptions
    {
        [Value(1, Required = true, HelpText = "Release version to generate the release notes for.")]
        public string Release { get; set; }

        [Usage(ApplicationAlias = "release-notes-generator")]
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
