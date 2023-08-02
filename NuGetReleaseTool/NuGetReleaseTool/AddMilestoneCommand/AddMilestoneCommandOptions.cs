using CommandLine.Text;
using CommandLine;

namespace NuGetReleaseTool.AddMilestoneCommand
{
    [Verb("add-milestone", HelpText = "Add milestones for issues closed for a release.")]
    public class AddMilestoneCommandOptions : BaseOptions
    {
        [Value(0, Required = true, HelpText = "Version to find issues for and add milestones. This argument doubles as the milestone description")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Release { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Option("end-commit", Required = false, HelpText = "The end commit for the release. This commit must be on the release branch. " +
            "You do not normally need to use this argument, unless there's a commit on the branch, that is not within the current release.")]
        public string? EndCommit { get; set; }

        [Option("dry-run", Required = false, HelpText = "Perform a dry-run and list all of the issues that will get a milestone.")]
        public bool DryRun { get; set; }

        [Option("correct-milestones", Required = false, HelpText = "Correct milestones for issues that have a milestone different from the expected one.")]
        public bool CorrectMilestones { get; set; }

        [Usage(ApplicationAlias = "add-milestone")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example("Add milestones to issues for a particular release", new AddMilestoneCommandOptions { Release = "6.3", GitHubToken = "asdf", EndCommit = "endSha" })
                };
            }
        }
    }
}
