using NuGetReleaseTool.GenerateInsertionChangelogCommand;
using Octokit;

namespace NuGetReleaseTool.AddMilestoneCommand
{
    public class AddMilestoneCommand
    {
        private readonly GitHubClient GitHubClient;
        private readonly AddMilestoneCommandOptions Options;

        public AddMilestoneCommand(AddMilestoneCommandOptions opts, GitHubClient gitHubClient)
        {
            Options = opts;
            GitHubClient = gitHubClient;
        }


        public async Task RunAsync()
        {
            var githubCommits = await Helpers.GetCommitsForRelease(GitHubClient, Options.Release, Options.EndCommit);
            List<CommitWithDetails> commits = await Helpers.GetCommitDetails(
                GitHubClient,
                Constants.NuGet,
                Constants.NuGetClient,
                issueRepositories: new string[] { "NuGet/Home", "NuGet/Client.Engineering" },
                githubCommits);

            var homeRepoIssueNumbers = new List<Tuple<int, string>>();

            foreach (var commit in commits)
            {
                foreach (var issue in commit.Issues)
                {
                    if (issue.Item2.Contains("nuget/home", StringComparison.OrdinalIgnoreCase))
                    {
                        homeRepoIssueNumbers.Add(issue);
                    }
                }
            }

            Milestone expectedMilestone = await GetExpectedMilestone();

            foreach (var homeIssue in homeRepoIssueNumbers)
            {
                var issue = await GitHubClient.Issue.Get("nuget", "home", homeIssue.Item1);
                if (issue.Milestone?.Title != Options.Release)
                {
                    var toUpdate = issue.ToUpdate();
                    toUpdate.Milestone = expectedMilestone.Number;
                    await GitHubClient.Issue.Update("nuget", "home", homeIssue.Item1, toUpdate);
                }

            }

            async Task<Milestone> GetExpectedMilestone()
            {
                IReadOnlyList<Milestone> allMilestones = await GitHubClient.Issue.Milestone.GetAllForRepository("NuGet", "Home");
                return allMilestones.SingleOrDefault(e => e.Title == Options.Release) ??
                    throw new InvalidOperationException($"Could not locate a matching milestone with the title {Options.Release}");
            }
        }
    }
}
