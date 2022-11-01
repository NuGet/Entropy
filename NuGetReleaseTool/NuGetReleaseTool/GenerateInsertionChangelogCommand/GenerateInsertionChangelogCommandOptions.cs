using CommandLine;

namespace NuGetReleaseTool.GenerateInsertionChangelogCommand
{
    [Verb("generate-insertion-changelog", HelpText = "Generate an insertion changelog.")]
    public class GenerateInsertionChangelogCommandOptions : BaseOptions
    {
        [Option("startSha", Required = true, HelpText = "The sha from which to start the generator.")]
        public string StartSha { get; set; }

        [Option("branch", Required = true, HelpText = "The branch in which to search for the start sha.")]
        public string Branch { get; set; }

        [Option("output", Required = false, HelpText = "Directory to output the results file in.")]
        public string Output { get; set; }
    }
}
