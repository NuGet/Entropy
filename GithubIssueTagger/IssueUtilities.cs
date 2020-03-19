using Octokit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GithubIssueTagger
{
    internal class IssueUtilities
    {
        private static DateTimeOffset SixMonthsFromAppStartup = new DateTimeOffset(DateTime.Now.AddDays(-180.0));

        public static bool HasTestLabel(Issue issue)
        {
            return issue.State == ItemState.Open && issue.Labels.Any(e => e.Name.Equals("Area:Test"));
        }

        public static async Task AddLabelToMatchingIssues(GitHubClient client, string label, string org, string repo, Predicate<Issue> predicate)
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

        public static async Task RemoveLabelFromAllIssuesAsync(GitHubClient client, string label, string org, string repo)
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
