using CommandLine;

namespace NuGetReleaseTool.GenerateInsertionChangelogCommand
{
    [Verb("generate-insertion-changelog", HelpText = "Generate an insertion changelog.")]
    public class GenerateInsertionChangelogCommandOptions : BaseOptions
    {
        [Option("start-commit", Required = true, HelpText = "The sha from which to start the generator.")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string StartCommit { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Option("branch", Required = true, HelpText = "The branch in which to search for the start sha.")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Branch { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Option("output", Required = false, HelpText = "Directory to output the results file in.")]
        public string? Output { get; set; }
    }
}
