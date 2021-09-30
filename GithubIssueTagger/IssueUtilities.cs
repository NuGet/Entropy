using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GithubIssueTagger
{
    internal partial class IssueUtilities
    {
        private static DateTimeOffset SixMonthsFromAppStartup = new DateTimeOffset(DateTime.Now.AddDays(-180.0));

        public static async Task<IList<Issue>> GetIssuesForMilestone(GitHubClient client, string org, string repo, string milestone, Predicate<Issue> predicate)
        {
            var shouldPrioritize = new RepositoryIssueRequest
            {
                Milestone = milestone,
                Filter = IssueFilter.All,
            };

            var issuesForMilestone = await client.Issue.GetAllForRepository(org, repo, shouldPrioritize);

            return issuesForMilestone.Where(e => predicate(e)).ToList();
        }

        public static async Task<IList<Issue>> GetIssuesForLabel(GitHubClient client, string org, string repo, string label)
        {
            var issuesForMilestone = await GetAllIssues(client, org, repo);
            return issuesForMilestone.Where(e => HasLabel(e, label)).ToList();
        }

        public static async Task<IEnumerable<Issue>> GetAllIssues(GitHubClient client, string org, string repo)
        {
            var shouldPrioritize = new RepositoryIssueRequest
            {
                Filter = IssueFilter.All
            };

            var issuesForMilestone = await client.Issue.GetAllForRepository(org, repo, shouldPrioritize);
            return issuesForMilestone;
        }

        public static async Task<IEnumerable<Issue>> GetOpenPriority1Issues(GitHubClient client, string org, string repo)
        {
            var nugetRepos = new RepositoryCollection();
            nugetRepos.Add(org, repo);

            var queryLabels = new string[] { "priority:1" };

            var request = new SearchIssuesRequest()
            {
                Repos = nugetRepos,
                State = ItemState.Open,
                Labels = queryLabels
            };
            var issuesForMilestone = await client.Search.SearchIssues(request);
            return issuesForMilestone.Items;
        }

        /// <summary>
        /// Get all the issues considered unprocessed. This means that either the issue does not have any labels, or only has the pipeline labels.
        /// </summary>
        public static async Task<IList<Issue>> GetUnprocessedIssues(GitHubClient client, string org, string repo)
        {
            var shouldPrioritize = new RepositoryIssueRequest
            {
                Filter = IssueFilter.All
            };

            var issuesForMilestone = await client.Issue.GetAllForRepository(org, repo, shouldPrioritize);

            return issuesForMilestone.Where(e => IsUnprocessed(e)).ToList();

            static bool IsUnprocessed(Issue e)
            {
                return e.Labels.Count == 0 || e.Labels.All(e => e.Name.StartsWith("Pipeline")); 
            }
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

        private static bool HasLabel(Issue e, string label)
        {
            return e.Labels.Any(e => e.Name.Equals(label));
        }
    }
}
