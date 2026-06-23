using GithubIssueTagger.GraphQL;
using GithubIssueTagger.Reports.IceBox.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports.IceBox
{
    [CommandFactory(typeof(IceBoxReportCommandFactory))]
    internal class IceBoxReport : IReport
    {
        private readonly GitHubGraphQLClient _client;
        private string? _addLabelId;

        public IceBoxReport(GitHubGraphQLClient client)
        {
            _client = client;
        }

        public Task RunAsync()
        {
            Console.WriteLine("IceBox is not available in interactive mode");
            return Task.CompletedTask;
        }

        public async Task RunAsync(string owner, string repo, string label, int upvoteCount, string? add, IReadOnlyList<int>? issueNumbers = null, bool verbose = false)
        {
            Console.WriteLine($"IceBox watcher built from commit {GetBuildCommitHash()}");

            bool byNumber = issueNumbers != null && issueNumbers.Count > 0;

            await foreach (GetIssuesResult.IssuesModel issue in GetIssuesAsync(owner, repo, label, upvoteCount, issueNumbers))
            {
                // When issues are requested explicitly by number, the search label filter that the
                // "all issues" query relies on was not applied, so validate that the issue actually has the
                // search label. Without it there is no cutoff date, so it should be ignored (the caller most
                // likely passed the wrong issue number).
                if (byNumber
                    && !issue.Labels.Nodes.Any(l => string.Equals(l.Name, label, StringComparison.OrdinalIgnoreCase))
                    && !issue.Labels.PageInfo.HasNextPage)
                {
                    WriteGitHubActionsWarning($"Issue {issue.Number} does not have the '{label}' label, skipping.");
                    continue;
                }

                if (add != null)
                {
                    if (issue.Labels.Nodes.Any(l => l.Name == add))
                    {
                        if (_addLabelId == null)
                        {
                            Label addLabel = issue.Labels.Nodes.First(l => l.Name == add);
                            _addLabelId = addLabel.Id;
                        }

                        if (verbose)
                        {
                            Console.WriteLine($"Issue #{issue.Number} already has the '{add}' label, skipping.");
                        }

                        // action label already applied, skip
                        continue;
                    }
                    else if (issue.Labels.PageInfo.HasNextPage)
                    {
                        // TODO: Handle when issue has more than 100 labels
                        WriteGitHubActionsWarning("Unsupported scenario: issue " + issue.Number + " has more than 100 labels");
                    }
                }

                if (!TryGetCutoffDate(issue.TimelineItems.Nodes, label, add, out DateTime? labelAdded))
                {
                    labelAdded = await GetCutoffDateAsync(issue.Id, label, add);
                    if (labelAdded == null)
                    {
                        // TODO: If we reach here, we need to do a different GraphQL query to get more events for this issue to find the last time the label was added.
                        WriteGitHubActionsWarning("Unsupported scenario: issue " + issue.Number + " did not find label in latest events");
                        continue;
                    }
                }

                int upvotes = GetUpvoteCount(issue, labelAdded.Value, out bool hasCompleteCount);
                string upvoteText = hasCompleteCount ? upvotes.ToString() : upvotes + "+";
                string cutoffText = $"cutoff date {labelAdded.Value:yyyy-MM-dd} Upvotes: {upvoteText}";

                if (verbose)
                {
                    Console.WriteLine($"Issue #{issue.Number} {cutoffText}");
                }

                if (HasEnoughPositiveReactions(issue, labelAdded.Value, upvoteCount))
                {
                    Console.WriteLine($"Issue {issue.Number} has enough upvotes ({cutoffText})");
                    if (add != null)
                    {
                        if (issue.Closed)
                        {
                            Console.WriteLine($"Issue #{issue.Number} is closed, not adding the '{add}' label.");
                        }
                        else
                        {
                            if (_addLabelId == null)
                            {
                                _addLabelId = await GetLabelIdAsync(owner, repo, add);
                            }

                            await AddLabelToIssueAsync(issue.Id, _addLabelId);
                        }
                    }
                }
            }
        }

        private async IAsyncEnumerable<GetIssuesResult.IssuesModel> GetIssuesAsync(string owner, string repo, string label, int upvotes, IReadOnlyList<int>? issueNumbers)
        {
            if (issueNumbers != null && issueNumbers.Count > 0)
            {
                await foreach (GetIssuesResult.IssuesModel issue in GetIssuesByNumberAsync(owner, repo, upvotes, issueNumbers))
                {
                    yield return issue;
                }

                yield break;
            }

            // See GitHub docs on resource/query limits. Increasing the counts has a multiplactive effect towards the hourly query limit.
            Dictionary<string, object?>? variables = new Dictionary<string, object?>()
            {
                ["owner"] = owner,
                ["repo"] = repo,
                ["after"] = null,
                ["label"] = label,
                ["timelineCount"] = 10, // search label add / triage label removal are usually among the most recent events
                ["reactionCount"] = upvotes * 2
            };

            var request = new GraphQLRequest(IceBoxResource.GetIssues)
            {
                Variables = variables
            };

            while (variables != null)
            {
                GraphQLResponse<GetIssuesResult>? response = await _client.SendAsync<GetIssuesResult>(request);

                if (response?.Errors?.Count > 0)
                {
                    WriteGraphQlErrors(response.Errors);
                }

                if (response?.Data?.Repository.Issues.Nodes != null)
                {
                    foreach (GetIssuesResult.IssuesModel issue in response.Data.Repository.Issues.Nodes)
                    {
                        yield return issue;
                    }
                }

                var pageInfo = response?.Data?.Repository.Issues.PageInfo;
                if (pageInfo?.HasNextPage == true && pageInfo?.EndCursor != null)
                {
                    variables["after"] = pageInfo.EndCursor;
                }
                else
                {
                    variables = null;
                }
            }
        }

        private async IAsyncEnumerable<GetIssuesResult.IssuesModel> GetIssuesByNumberAsync(string owner, string repo, int upvotes, IReadOnlyList<int> issueNumbers)
        {
            foreach (int number in issueNumbers)
            {
                var variables = new Dictionary<string, object?>()
                {
                    ["owner"] = owner,
                    ["repo"] = repo,
                    ["number"] = number,
                    ["timelineCount"] = 10, // search label add / triage label removal are usually among the most recent events
                    ["reactionCount"] = upvotes * 2
                };

                var request = new GraphQLRequest(IceBoxResource.GetIssueByNumber)
                {
                    Variables = variables
                };

                GraphQLResponse<GetIssueResult>? response = await _client.SendAsync<GetIssueResult>(request);

                if (response?.Errors?.Count > 0)
                {
                    WriteGraphQlErrors(response.Errors);
                }

                GetIssuesResult.IssuesModel? issue = response?.Data?.Repository.Issue;
                if (issue == null)
                {
                    WriteGitHubActionsWarning("Issue " + number + " was not found.");
                    continue;
                }

                yield return issue;
            }
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

        private async Task<DateTime?> GetCutoffDateAsync(string issueId, string searchLabel, string? triageLabel)
        {
            Dictionary<string, object?> variables = new Dictionary<string, object?>()
            {
                ["issue"] = issueId
            };

            var request = new GraphQLRequest(IceBoxResource.GetLabeledEvents)
            {
                Variables = variables
            };

            GraphQLResponse<GetLabeledEventsResult>? response = await _client.SendAsync<GetLabeledEventsResult>(request);

            if (response == null)
            {
                return null;
            }

            if (response?.Errors?.Count > 0)
            {
                WriteGraphQlErrors(response.Errors);
            }

            if (response?.Data == null)
            {
                Console.WriteLine("GetLabeledEvents query failed:");
                if (response?.Errors != null)
                {
                    foreach (var error in response.Errors)
                    {
                        Console.WriteLine(error.Message);
                    }
                }
                return null;
            }

            IReadOnlyList<TimelineEvent>? events = response.Data?.Node.TimelineItems.Nodes;
            if (!TryGetCutoffDate(events, searchLabel, triageLabel, out DateTime? cutoffDate))
            {
                return null;
            }

            return cutoffDate;
        }

        private static bool HasEnoughPositiveReactions(GetIssuesResult.IssuesModel issue, DateTime after, int upvotes)
        {
            if (!issue.Reactions.PageInfo.HasNextPage)
            {
                // No need to fetch more reactions, since we already have the complete list.
                int count = GetCustomerUpvoteCount(issue.Reactions.Nodes.Where(r => r.CreatedAt > after));
                return count >= upvotes;
            }
            else
            {
                // If few customers added multiple reactions, we should have enough information already
                int count = GetCustomerUpvoteCount(issue.Reactions.Nodes.Where(r => r.CreatedAt > after));
                if (count > upvotes)
                {
                    return true;
                }
                else
                {
                    // If the oldest date we already have is more recent than the cutoff, then getting more reactions will not help
                    DateTime? min = issue.Reactions.Nodes.Select(r => r.CreatedAt).MinOrDefault();
                    if (min != null && min < after)
                    {
                        return false;
                    }

                    // TODO: Need to get more reactions from GraphQL to check if upvote threshold met
                    WriteGitHubActionsWarning("Unsupported scenario: issue " + issue?.Number + " needs to check more reactions for threshold check.");
                    return false;
                }
            }
        }

        // Counts distinct customers who left a positive reaction after the cutoff date, from the reactions
        // already fetched for the issue. Returns false in hasCompleteCount when more reactions exist than were
        // fetched and could change the result (so callers can indicate the count is a lower bound).
        private static int GetUpvoteCount(GetIssuesResult.IssuesModel issue, DateTime after, out bool hasCompleteCount)
        {
            int count = GetCustomerUpvoteCount(issue.Reactions.Nodes.Where(r => r.CreatedAt > after));

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

            return count;
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
            if (string.Equals("THUMBS_UP", reaction, StringComparison.OrdinalIgnoreCase)
                || string.Equals("HEART", reaction, StringComparison.OrdinalIgnoreCase)
                || string.Equals("ROCKET", reaction, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<string?> GetLabelIdAsync(string owner, string repo, string label)
        {
            var variables = new Dictionary<string, object?>()
            {
                ["owner"] = owner,
                ["repo"] = repo,
                ["label"] = label
            };

            var request = new GraphQLRequest(IceBoxResource.GetLabelId)
            {
                Variables = variables
            };

            GraphQLResponse<GetLabelIdResult>? response = await _client.SendAsync<GetLabelIdResult>(request);
            if (response?.Errors?.Count > 0)
            {
                WriteGraphQlErrors(response.Errors);
            }

            string? id = response?.Data?.Repository?.Label?.Id;

            if (id == null)
            {
                WriteGitHubActionsWarning("Unsupported scenario: GetLabelIdAsync failed.");
            }

            return id;
        }

        private async Task AddLabelToIssueAsync(string id, string? addLabelId)
        {
            var variables = new Dictionary<string, object?>()
            {
                ["issue"] = id,
                ["label"] = addLabelId
            };

            var request = new GraphQLRequest(IceBoxResource.AddLabelToIssue)
            {
                Variables = variables
            };

            var response = await _client.SendAsync<object>(request);

            if (response?.Errors?.Count > 0)
            {
                WriteGitHubActionsWarning("Unsupported scenario: AddLabelToIssue failed.");
                WriteGraphQlErrors(response.Errors);
            }
        }

        private static void WriteGitHubActionsWarning(string message)
        {
            Console.WriteLine("::warning ::" + message);
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

        private static void WriteGraphQlErrors(IReadOnlyList<GraphQLResponseError> errors)
        {
            foreach (var error in errors)
            {
                if (error.Message!= null)
                {
                    WriteGitHubActionsError(error.Message);
                }
            }
        }

        private static void WriteGitHubActionsError(string message)
        {
            Console.WriteLine("::error ::" + message);
        }

        private class IceBoxReportCommandFactory : ICommandFactory
        {
            public Command CreateCommand(Type type, GitHubPatBinder patBinder)
            {
                var command = new Command("IceBox");
                command.Description = "Check for issues with a label that exceed a count of upvotes since the label was added.";

                var ownerOption = new Option<string>("--owner");
                ownerOption.AddAlias("-o");
                ownerOption.Description = "GitHub owner (org or user) of the repo.";
                ownerOption.SetDefaultValue("NuGet");
                command.Add(ownerOption);

                var repoOption = new Option<string>("--repo");
                repoOption.AddAlias("-r");
                repoOption.Description = "Repo to search issues in.";
                repoOption.SetDefaultValue("Home");
                command.Add(repoOption);

                var labelOption = new Option<string>("--label");
                labelOption.AddAlias("-l");
                labelOption.Description = "Which label to filter issues by.";
                labelOption.SetDefaultValue("pipeline:IceBox");
                command.Add(labelOption);

                var upvotesOption = new Option<int>("--upvotes");
                upvotesOption.AddAlias("-u");
                upvotesOption.Description = "Number of upvotes required to meet threshold.";
                upvotesOption.SetDefaultValue(5);
                command.Add(upvotesOption);

                var addOption = new Option<string>("--add");
                addOption.AddAlias("-a");
                addOption.Description = "Label to add on issues which meet or exceed the upvote threshold. When not specified, no action is taken.";
                command.Add(addOption);

                var issueOption = new Option<int[]>("--issue");
                issueOption.AddAlias("-i");
                issueOption.Description = "Specific issue number(s) to process instead of every issue with the label. Can be repeated or given multiple values. Useful for debugging.";
                issueOption.AllowMultipleArgumentsPerToken = true;
                command.Add(issueOption);

                var verboseOption = new Option<bool>("--verbose");
                verboseOption.AddAlias("-v");
                verboseOption.Description = "Output the cutoff date and upvote count for every processed issue.";
                command.Add(verboseOption);

                command.SetHandler(async
                    (GitHubPat pat,
                    string owner,
                    string repo, 
                    string label, 
                    int upvotes,
                    string add,
                    int[] issues,
                    bool verbose) =>
                {
                    var serviceProvider = new ServiceCollection()
                        .AddGithubIssueTagger(pat)
                        .BuildServiceProvider();

                    var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
                    using (scopeFactory.CreateScope())
                    {
                        var report = serviceProvider.GetRequiredService<IceBoxReport>();
                        await report.RunAsync(owner, repo, label, upvotes, add, issues, verbose);
                    }
                }, patBinder, ownerOption, repoOption, labelOption, upvotesOption, addOption, issueOption, verboseOption);

                return command;
            }
        }
    }
}
