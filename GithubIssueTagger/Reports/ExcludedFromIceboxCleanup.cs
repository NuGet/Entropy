using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports
{
    internal class ExcludedFromIceboxCleanup : IReport
    {
        private GitHubClient _client;
        private DateTimeOffset _date;
        private string ExcludedFromIcebox = "Status:Excluded from icebox cleanup";
        private string Inactive = "Status:Inactive";

        public ExcludedFromIceboxCleanup(GitHubClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _date = new DateTimeOffset(new DateTime(2024, 2, 27));
        }

        public async Task RunAsync()
        {
            bool dryRun = true;

            var excludedFromCleanupIssues = await IssueUtilities.GetIssuesForLabelsAsync(_client, "NuGet", "Home", ItemStateFilter.Open, ExcludedFromIcebox);

            List<Issue> toKeep = new List<Issue>();
            List<Issue> toClose = new List<Issue>();

            foreach (var issue in excludedFromCleanupIssues)
            {
                bool keepIssue = false;

                if (issue.Comments > 0)
                {
                    IReadOnlyList<IssueComment> issueComments = await _client.Issue.Comment.GetAllForIssue("NuGet", "Home", issue.Number);
                    DateTimeOffset latestDate = issueComments.Max(e => e.CreatedAt);
                    if (latestDate > _date)
                    {
                        keepIssue = true;
                    }
                }

                if (keepIssue)
                {
                    toKeep.Add(issue!);
                }
                else
                {
                    toClose.Add(issue!);
                }
            }

            if (!dryRun)
            {
                foreach (var issue in toClose)
                {
                    var issueUpdate = issue.ToUpdate();
                    issueUpdate.RemoveLabel(ExcludedFromIcebox);
                    await _client.Issue.Update("NuGet", "Home", issue.Number, issueUpdate);
                    Console.WriteLine($"Updated issue: {issue.HtmlUrl}");

                }

                foreach (var issue in toKeep)
                {
                    var issueUpdate = issue.ToUpdate();
                    issueUpdate.RemoveLabel(ExcludedFromIcebox);
                    issueUpdate.RemoveLabel(Inactive);
                    await _client.Issue.Update("NuGet", "Home", issue.Number, issueUpdate);
                    Console.WriteLine($"Updated issue: {issue.HtmlUrl}");
                }
            }
            else
            {
                Console.WriteLine("Closing " + toClose.Count);
                foreach (var issue in toClose)
                {
                    Console.WriteLine($"{issue.HtmlUrl}");
                }

                Console.WriteLine("Keeping " + toKeep.Count);

                foreach (var issue in toKeep)
                {
                    Console.WriteLine($"{issue.HtmlUrl}");
                }
            }

        }

    }
}
