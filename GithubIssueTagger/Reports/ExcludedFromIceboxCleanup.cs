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

            List<Issue> removeInactiveLabel = new List<Issue>();
            List<Issue> removeExclusionLabel = new List<Issue>();

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

                    //if(issue.Comments > 10)
                    //{
                    //    keepIssue = true;
                    //}

                    //if (issue.Reactions.TotalCount > 1)
                    //{
                    //    keepIssue = true;
                    //}
                }

                if (keepIssue)
                {
                    removeInactiveLabel.Add(issue!);
                }
                else
                {
                    removeExclusionLabel.Add(issue!);
                }
            }

            if (!dryRun)
            {
                int i = 0;

                foreach (var issue in removeExclusionLabel)
                {
                    var issueUpdate = issue.ToUpdate();
                    issueUpdate.RemoveLabel(ExcludedFromIcebox);
                    await _client.Issue.Update("NuGet", "Home", issue.Number, issueUpdate);
                    Console.WriteLine($"Updated issue: {issue.HtmlUrl}");
                }

                foreach (var issue in removeInactiveLabel)
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
                Console.WriteLine("Removing exclusion label. This issues will be scheduled for clsoing " + removeExclusionLabel.Count);
                foreach (var issue in removeExclusionLabel)
                {
                    Console.WriteLine($"{issue.HtmlUrl}");
                }

                Console.WriteLine("Removing only exclusion and inactive labels. These issues will go into the 2 year inactive period required to be closed. " + removeInactiveLabel.Count);

                foreach (var issue in removeInactiveLabel)
                {
                    Console.WriteLine($"{issue.HtmlUrl}");
                }
            }

        }

    }
}
