using NuGetReleaseTool.GenerateInsertionChangelogCommand;
using NuGetReleaseTool.GenerateReleaseNotesCommand;
using Octokit;
using System.Collections.Immutable;

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

            var homeRepoIssueNumbers = new HashSet<Tuple<int, string>>();

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

            Milestone expectedMilestone = await GetExpectedMilestoneAsync();

            foreach (var homeIssue in homeRepoIssueNumbers.ToImmutableSortedSet())
            {
                var issue = await GitHubClient.Issue.Get("nuget", "home", homeIssue.Item1);

                if (issue.State == ItemState.Open && !Options.AddToOpenIssues)
                {
                    continue;
                }

                if (issue.Labels.Any(l => l.Name.Equals(IssueLabels.Docs)) || issue.Labels.Any(l => l.Name.Equals(IssueLabels.DeveloperDocs)))
                {
                    continue;
                }

                if (!Options.DryRun)
                {
                    if (issue.Milestone?.Title == null)
                    {
                        await AddMilestoneToIssueAsync(expectedMilestone, issue);
                    }
                    if (issue.Milestone?.Title != expectedMilestone.Title && Options.CorrectMilestones)
                    {
                        await AddMilestoneToIssueAsync(expectedMilestone, issue);
                    }
                }
                else
                {
                    if (issue.Milestone?.Title != expectedMilestone.Title)
                    {
                        Console.WriteLine($"{issue.HtmlUrl} {issue.State} Expected: {expectedMilestone.Title} Actual: {issue.Milestone?.Title}");
                    }
                }
            }

            async Task<Milestone> GetExpectedMilestoneAsync()
            {
                IReadOnlyList<Milestone> allMilestones = await GitHubClient.Issue.Milestone.GetAllForRepository("NuGet", "Home");
                return allMilestones.SingleOrDefault(e => e.Title == Options.Release) ??
                    throw new InvalidOperationException($"Could not locate a matching milestone with the title {Options.Release}");
            }
        }

        private async Task AddMilestoneToIssueAsync(Milestone expectedMilestone, Issue issue)
        {
            try
            {
                var toUpdate = issue.ToUpdate();
                toUpdate.Milestone = expectedMilestone.Number;
                await GitHubClient.Issue.Update("nuget", "home", issue.Number, toUpdate);
                Console.WriteLine($"Added {expectedMilestone.Title} to {issue.HtmlUrl}");
            } catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed updating {issue.HtmlUrl}.");
                Console.Error.WriteLine(ex);
            }
        }
    }
}
