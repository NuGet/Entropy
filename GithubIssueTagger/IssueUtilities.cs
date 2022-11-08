using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#nullable disable

namespace GithubIssueTagger
{
    internal partial class IssueUtilities
    {
        public static async Task GetIssuesRankedAsync(GitHubClient client, params string[] labels)
        {
            IList<Issue> issues = await GetIssuesForAnyMatchingLabelsAsync(client, "NuGet", "Home", labels);

            var allIssues = new List<IssueRankingModel>(issues.Count);

            var internalAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "nkolev92",
                "donnie-msft",
                "dominofire",
                "erdembayar",
                "zivkan",
                "clairernovotny",
                "erdembayar",
                "jeffkl",
                "rrelyea",
                "jebriede",
                "dtivel",
                "heng-liu",
                "kartheekp-ms",
                "aortiz-msft",
                "zkat",
                "emgarten",
                "patobeltran",
                "rohit21agrawal",
                "zhili1208",
                "jainaashish",
                "cristinamanum"
            };

            foreach (var issue in issues)
            {
                var (score,upvotes, comments)  = await CalculateScoreAsync(issue, client, internalAliases);

                allIssues.Add(new IssueRankingModel(issue, score, upvotes, comments));
            }

            var markdownTable = allIssues.OrderByDescending(e => e.Upvotes).ToMarkdownTable(GetModelMapping());

            Console.WriteLine(markdownTable);

            static List<Tuple<string, string>> GetModelMapping()
            {
                return new List<Tuple<string, string>>()
                {
                    new Tuple<string, string>("Link", "Link"),
                    new Tuple<string, string>("Title", "Title"),
                    new Tuple<string, string>("Assignee", "Assignee"),
                    new Tuple<string, string>("Milestone", "Milestone"),
                    new Tuple<string, string>("Score", "Score"),
                    new Tuple<string, string>("Upvotes", "Upvotes"),
                    new Tuple<string, string>("Comments", "Comments"),
                    new Tuple<string, string>("Type", "Type"),
                    new Tuple<string, string>("Cost", "Cost"),
                    new Tuple<string, string>("Verdict", "Verdict"),
                };
            }

            static async Task<(double, int, int)> CalculateScoreAsync(Issue issue, GitHubClient client, HashSet<string> internalAliases)
            {
                int totalCommentsCount = issue.Comments;
                int reactionCount = issue.Reactions.TotalCount;

                IReadOnlyList<IssueComment> issueComments = await client.Issue.Comment.GetAllForIssue("NuGet", "Home", issue.Number);

                List<string> allCommenters = issueComments.Select(e => e.User.Login).ToList();
                List<string> uniqueCommenterList = allCommenters.Distinct<string>().Where(e => e.Equals(issue.User.Login)).ToList();
                int uniqueCommentersCount = uniqueCommenterList.Count;

                var internalCommentersCount = uniqueCommenterList.Where(e => internalAliases.Contains(e) && !internalAliases.Contains(issue.User.Login)).Count();

                return (uniqueCommentersCount + reactionCount - (internalCommentersCount * 0.25) + CaculateExtraCommentImpact(totalCommentsCount, uniqueCommentersCount), 
                    reactionCount, 
                    totalCommentsCount);
                static double CaculateExtraCommentImpact(int totalComments, int uniqueCommenters)
                {
                    int diff = totalComments - uniqueCommenters;

                    var tens = Math.Max(Math.Min(diff - 10, 10), 0) * 0.25;
                    var twenties = Math.Max(Math.Min(diff - 20, 10), 0) * 0.10;
                    var thirties = Math.Max(diff - 30, 0) * 0.05;

                    return tens + twenties + thirties;
                }

            }
        }

        internal static async Task ReopenAutoclosedDocsRepoIssuesAsync(GitHubClient client)
        {
            var nugetRepos = new RepositoryCollection();
            string owner = "nuget";
            string repoName = "docs.microsoft.com-nuget";
            nugetRepos.Add(owner, repoName);

            var queryLabels = new string[] { "autoclose" };
            var dateRange = DateRange.GreaterThanOrEquals(new DateTimeOffset(year: 2022, month: 4, day: 15, hour: 0, minute: 0, second: 0, TimeSpan.Zero));


            var request = new SearchIssuesRequest()
            {
                Repos = nugetRepos,
                State = ItemState.Closed,
                Labels = queryLabels,
                Updated = dateRange
            };
            var issuesForMilestone = await client.Search.SearchIssues(request);
            var allIssues =  issuesForMilestone.Items;

            foreach(var issue in allIssues)
            {
                var issueToUpdate = issue.ToUpdate();
                issueToUpdate.State = ItemState.Open;
                issueToUpdate.RemoveLabel("autoclose");
                await client.Issue.Update(owner, repoName, issue.Number, issueToUpdate);
                Console.WriteLine($"Reopening autoclosed issue : {issue.Url}");
            }
        }

        public static async Task<IList<Issue>> GetIssuesForMilestoneAsync(GitHubClient client, string org, string repo, string milestone, Predicate<Issue> predicate)
        {
            var shouldPrioritize = new RepositoryIssueRequest
            {
                Milestone = milestone,
                Filter = IssueFilter.All,
            };

            var issuesForMilestone = await client.Issue.GetAllForRepository(org, repo, shouldPrioritize);

            return issuesForMilestone.Where(e => predicate(e)).ToList();
        }

        public static Task<IList<Issue>> GetIssuesForLabelsAsync(GitHubClient client, string org, string repo, params string[] labels)
        {
            return GetIssuesForLabelsAsync(client, org, repo, ItemStateFilter.All, labels);
        }

        public static async Task<IList<Issue>> GetIssuesForLabelsAsync(GitHubClient client, string org, string repo, ItemStateFilter itemState, params string[] labels)
        {
            var shouldPrioritize = new RepositoryIssueRequest
            {
                Filter = IssueFilter.All,
                State = itemState,
            };

            foreach (var label in labels)
            {
                shouldPrioritize.Labels.Add(label);
            }
            var issues = await client.Issue.GetAllForRepository(org, repo, shouldPrioritize);
            return issues.ToList();
        }

        public static async Task<IList<Issue>> GetIssuesForAnyMatchingLabelsAsync(GitHubClient client, string org, string repo, params string[] labels)
        {
            var issuesForMilestone = await GetAllIssuesAsync(client, org, repo);
            return issuesForMilestone.Where(e => labels.Any(label => HasLabel(e, label))).ToList();
        }

        public static async Task<IReadOnlyList<Issue>> GetAllIssuesAsync(GitHubClient client, string org, string repo)
        {
            var shouldPrioritize = new RepositoryIssueRequest
            {
                Filter = IssueFilter.All
            };

            var issuesForMilestone = await client.Issue.GetAllForRepository(org, repo, shouldPrioritize);
            return issuesForMilestone;
        }

        public static async Task<IReadOnlyList<Issue>> GetOpenPriority1IssuesAsync(GitHubClient client, string org, string repo)
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
        public static async Task<IList<Issue>> GetUnprocessedIssuesAsync(GitHubClient client, string org, string repo)
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

        public static async Task AddLabelToMatchingIssuesAsync(GitHubClient client, string label, string org, string repo, Predicate<Issue> predicate)
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

    public class IssueRankingModel
    {
        public string Link { get; }
        public string Title { get; }
        public string Assignee { get; }
        public string Milestone { get; }
        public double Score { get; }
        public int Upvotes { get; }
        public double Comments { get; }
        public string Type { get; }
        public string Verdict { get; }
        public string Cost { get; }


        public IssueRankingModel(Issue e, double score, int upvotes, int comments)
        {
            Link = e.HtmlUrl;
            Title = e.Title;
            Assignee = string.Join(",", e.Assignees.Select(e => e.Login));
            Milestone = e.Milestone?.Title;
            Score = score;
            Upvotes = upvotes;
            Comments = comments;
            Type = e.Labels.Any(e => e.Name.Equals("Type:Feature")) ? "Feature" : "DCR";
        }
    }
}
