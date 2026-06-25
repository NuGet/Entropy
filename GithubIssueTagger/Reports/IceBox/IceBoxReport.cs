using GithubIssueTagger.GraphQL;
using GithubIssueTagger.Reports.IceBox.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports.IceBox
{
    [CommandFactory(typeof(IceBoxReportCommandFactory))]
    internal class IceBoxReport : IReport
    {
        // The search label add / triage label removal are usually among the most recent timeline events.
        private const int TimelineWindow = 10;

        private readonly IceBoxGitHubClient _client;
        private string? _addLabelId;

        public IceBoxReport(GitHubGraphQLClient client)
        {
            _client = new IceBoxGitHubClient(client);
        }

        public Task RunAsync()
        {
            Console.WriteLine("IceBox is not available in interactive mode");
            return Task.CompletedTask;
        }

        public async Task RunAsync(string owner, string repo, string label, int upvoteCount, string? add, IReadOnlyList<int>? issueNumbers = null, bool verbose = false)
        {
            Console.WriteLine($"IceBox watcher built from commit {GetBuildCommitHash()}");

            // When the caller passes explicit issue numbers, the search label filter that the bulk query
            // applies has not run, so each issue must be validated to actually have the search label.
            bool byNumber = issueNumbers != null && issueNumbers.Count > 0;
            IReadOnlyList<int> numbers = byNumber
                ? issueNumbers!
                : await _client.GetIssueNumbersAsync(owner, repo, label);

            foreach (int number in numbers)
            {
                IssuesModel? issue = await _client.GetIssueAsync(owner, repo, number, TimelineWindow, upvoteCount * 2);
                if (issue == null)
                {
                    GitHubActionsLog.Warning("Issue " + number + " was not found.");
                    continue;
                }

                await ProcessIssueAsync(issue, owner, repo, label, add, upvoteCount, byNumber, verbose);
            }
        }

        private async Task ProcessIssueAsync(IssuesModel issue, string owner, string repo, string label, string? add, int upvoteCount, bool byNumber, bool verbose)
        {
            // A wrong issue number was most likely passed: without the search label there is no cutoff date.
            if (byNumber && !HasLabel(issue, label))
            {
                GitHubActionsLog.Warning($"Issue {issue.Number} does not have the '{label}' label, skipping.");
                return;
            }

            if (add != null && AlreadyHasAddLabel(issue, add, verbose))
            {
                return;
            }

            DateTime? cutoff = await GetCutoffDateAsync(issue, label, add);
            if (cutoff == null)
            {
                // TODO: If we reach here, we need a different GraphQL query to get more events for this issue to find the last time the label was added.
                GitHubActionsLog.Warning("Unsupported scenario: issue " + issue.Number + " did not find label in latest events");
                return;
            }

            (int upvotes, bool hasCompleteCount) = CountUpvotes(issue, cutoff.Value);
            string upvoteText = hasCompleteCount ? upvotes.ToString() : upvotes + "+";
            string cutoffText = $"cutoff date {cutoff.Value:yyyy-MM-dd} Upvotes: {upvoteText}";

            if (verbose)
            {
                Console.WriteLine($"Issue #{issue.Number} {cutoffText}");
            }

            if (MeetsThreshold(upvotes, hasCompleteCount, upvoteCount, issue.Number))
            {
                Console.WriteLine($"Issue {issue.Number} has enough upvotes ({cutoffText})");
                if (add != null)
                {
                    await AddLabelAsync(issue, owner, repo, add);
                }
            }
        }

        // Returns true when the issue has the label, or when it might have it on a label page we did not
        // fetch (more than 100 labels), so we never skip an issue we are not certain about.
        private static bool HasLabel(IssuesModel issue, string label)
        {
            return issue.Labels.Nodes.Any(l => string.Equals(l.Name, label, StringComparison.OrdinalIgnoreCase))
                || issue.Labels.PageInfo.HasNextPage;
        }

        private bool AlreadyHasAddLabel(IssuesModel issue, string add, bool verbose)
        {
            Label? existing = issue.Labels.Nodes.FirstOrDefault(l => l.Name == add);
            if (existing != null)
            {
                _addLabelId ??= existing.Id;

                if (verbose)
                {
                    Console.WriteLine($"Issue #{issue.Number} already has the '{add}' label, skipping.");
                }

                return true;
            }

            if (issue.Labels.PageInfo.HasNextPage)
            {
                // TODO: Handle when issue has more than 100 labels
                GitHubActionsLog.Warning("Unsupported scenario: issue " + issue.Number + " has more than 100 labels");
            }

            return false;
        }

        private async Task AddLabelAsync(IssuesModel issue, string owner, string repo, string add)
        {
            if (issue.Closed)
            {
                Console.WriteLine($"Issue #{issue.Number} is closed, not adding the '{add}' label.");
                return;
            }

            _addLabelId ??= await _client.GetLabelIdAsync(owner, repo, add);
            await _client.AddLabelToIssueAsync(issue.Id, _addLabelId);
        }

        // Every issue is fetched via the single-issue query, whose timelineItems window (last N events) is
        // reliable. If the search label add is older than that window, TryGetCutoffDate returns false and we
        // fall back to a query that fetches a larger window of events.
        private async Task<DateTime?> GetCutoffDateAsync(IssuesModel issue, string searchLabel, string? triageLabel)
        {
            if (TryGetCutoffDate(issue.TimelineItems.Nodes, searchLabel, triageLabel, out DateTime? cutoff))
            {
                return cutoff;
            }

            IReadOnlyList<TimelineEvent>? events = await _client.GetTimelineEventsAsync(issue.Id);
            if (events != null && TryGetCutoffDate(events, searchLabel, triageLabel, out cutoff))
            {
                return cutoff;
            }

            return null;
        }

        // The cutoff date is the later of when the search label (e.g. Priority:3) was last added, or the last
        // time the triage label (e.g. Triage:NeedsTriageDiscussion) was removed. Only reactions after the cutoff
        // count toward the upvote threshold, so removing the triage label resets the count. Returns false when the
        // search label was not found in the supplied events, so the caller can fetch more.
        private static bool TryGetCutoffDate(IReadOnlyList<TimelineEvent>? events, string searchLabel, string? triageLabel, [NotNullWhen(true)] out DateTime? cutoffDate)
        {
            DateTime? searchLabelAdded = events
                ?.Where(e => e.IsLabeledEvent && string.Equals(searchLabel, e.Label?.Name, StringComparison.OrdinalIgnoreCase))
                .Select(e => (DateTime?)e.CreatedAt)
                .Max();

            if (searchLabelAdded == null)
            {
                cutoffDate = null;
                return false;
            }

            DateTime cutoff = searchLabelAdded.Value;

            if (triageLabel != null)
            {
                DateTime? triageLabelRemoved = events!
                    .Where(e => e.IsUnlabeledEvent && string.Equals(triageLabel, e.Label?.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(e => (DateTime?)e.CreatedAt)
                    .Max();

                if (triageLabelRemoved != null && triageLabelRemoved.Value > cutoff)
                {
                    cutoff = triageLabelRemoved.Value;
                }
            }

            cutoffDate = cutoff;
            return true;
        }

        // Counts distinct customers who left a positive reaction after the cutoff date, from the reactions
        // already fetched for the issue. hasCompleteCount is false when more reactions exist than were fetched
        // and could still change the result, so the count is only a lower bound.
        private static (int Count, bool HasCompleteCount) CountUpvotes(IssuesModel issue, DateTime after)
        {
            int count = GetCustomerUpvoteCount(issue.Reactions.Nodes.Where(r => r.CreatedAt > after));

            bool hasCompleteCount;
            if (!issue.Reactions.PageInfo.HasNextPage)
            {
                hasCompleteCount = true;
            }
            else
            {
                // If the oldest reaction we fetched is already older than the cutoff, we've seen every
                // reaction that could count, so the number is complete despite there being more reactions.
                DateTime? min = issue.Reactions.Nodes.Select(r => r.CreatedAt).MinOrDefault();
                hasCompleteCount = min != null && min < after;
            }

            return (count, hasCompleteCount);
        }

        // Decides whether the upvote count meets the required threshold. When the count is only a lower bound
        // (hasCompleteCount is false), the threshold can only be confirmed once it is strictly exceeded;
        // otherwise more reactions would need to be fetched to be sure.
        private static bool MeetsThreshold(int count, bool hasCompleteCount, int required, int? issueNumber)
        {
            if (hasCompleteCount)
            {
                return count >= required;
            }

            if (count > required)
            {
                return true;
            }

            // TODO: Need to get more reactions from GraphQL to check if upvote threshold met
            GitHubActionsLog.Warning("Unsupported scenario: issue " + issueNumber + " needs to check more reactions for threshold check.");
            return false;
        }

        private static int GetCustomerUpvoteCount(IEnumerable<Reaction> reactions)
        {
            HashSet<string> customers = new HashSet<string>();
            foreach (var reaction in reactions)
            {
                if (IsPositiveReaction(reaction.Content))
                {
                    customers.Add(reaction.User.Login);
                }
            }

            return customers.Count;
        }

        private static bool IsPositiveReaction(string? reaction)
        {
            return string.Equals("THUMBS_UP", reaction, StringComparison.OrdinalIgnoreCase)
                || string.Equals("HEART", reaction, StringComparison.OrdinalIgnoreCase)
                || string.Equals("ROCKET", reaction, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetBuildCommitHash()
        {
            string? informationalVersion = typeof(IceBoxReport).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            // SourceLink appends the commit hash after a '+' (e.g. "2026.6.23+abcdef0...").
            int plusIndex = informationalVersion?.IndexOf('+') ?? -1;
            if (plusIndex >= 0 && plusIndex + 1 < informationalVersion!.Length)
            {
                return informationalVersion.Substring(plusIndex + 1);
            }

            return "unknown";
        }
    }
}
