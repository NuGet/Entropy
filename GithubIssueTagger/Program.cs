using Octokit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GithubIssueTagger
{
    class Program
    {
        private const string token = "YourGithubToken";
        private static DateTimeOffset SixMonthsFromAppStartup = new DateTimeOffset(DateTime.Now.AddDays(-180.0));

        static async Task Main(string[] args)
        {
            var client = new GitHubClient(new ProductHeaderValue("nuget-github-issue-tagger"));
            client.Credentials = new Credentials(GetClientSecret());

            await RemoveLabelFromAllIssuesAsync(client, "ToBeMoved", "nuget", "home");

            Predicate<Issue> predicate = WasUpdatedInLastSixMonths;
            await AddLabelToMatchingIssues(client, "ToBeMoved", "nuget", "home", predicate);

            Console.WriteLine("Done!");
        }

        private static string GetClientSecret()
        {
            return token;
        }

        private static bool WasUpdatedInLastSixMonths(Issue issue)
        {
            return issue.State == ItemState.Open && issue.UpdatedAt.Value.DateTime < SixMonthsFromAppStartup;
        }

        private static async Task AddLabelToMatchingIssues(GitHubClient client, string label, string org, string repo, Predicate<Issue> predicate)
        {
            var issuesForRepo = await client.Issue.GetAllForRepository(org, repo);

            foreach (var issue in issuesForRepo)
            {
                if (predicate(issue))
                {
                    try
                    {
                        var issueUpdate = issue.ToUpdate();
                        issueUpdate.AddLabel(label);
                        await client.Issue.Update(org, repo, issue.Number, issueUpdate);
                        Console.WriteLine($"Updated issue: {issue.HtmlUrl}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Unhandled issue {issue.HtmlUrl} {e}");
                    }
                }
            }
        }

        private static async Task RemoveLabelFromAllIssuesAsync(GitHubClient client, string label, string org, string repo)
        {
            var issuesForRepo = await client.Issue.GetAllForRepository(org, repo);

            foreach (var issue in issuesForRepo)
            {
                if (issue.Labels != null)
                {
                    if (issue.Labels.Any(e => e.Name.Equals(label)))
                    {
                        try
                        {
                            var issueUpdate = issue.ToUpdate();
                            issueUpdate.RemoveLabel(label);
                            await client.Issue.Update(org, repo, issue.Number, issueUpdate);
                            Console.WriteLine($"Updated issue: {issue.HtmlUrl}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Unhandled issue {issue.HtmlUrl} {e}");
                        }
                    }
                }
            }
        }
    }
}
