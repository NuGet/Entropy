using GithubIssueTagger.GraphQL;
using GithubIssueTagger.Reports.IceBox.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports.IceBox
{
    [CommandFactory(typeof(IceBoxReportCommandFactory))]
    internal class IceBoxReport : IReport
    {
        private readonly GitHubGraphQLClient _client;
        private string? _triageLabelId;

        public IceBoxReport(GitHubGraphQLClient client)
        {
            _client = client;
        }

        public Task RunAsync()
        {
            Console.WriteLine("IceBox is not available in interactive mode");
            return Task.CompletedTask;
        }

        public async Task RunAsync(IceBoxConfig config, bool apply, bool verbose)
        {
            string searchLabel = config.SearchLabel;
            string triageLabel = config.Triage.Label;
            int upvoteCount = config.Triage.Upvotes;

            await foreach (GetIssuesResult.IssuesModel issue in GetIssuesAsync(config.Owner, config.Repo, searchLabel, upvoteCount))
            {
                if (issue.Labels.Nodes.Any(l => l.Name == triageLabel))
                {
                    if (_triageLabelId == null)
                    {
                        Label label = issue.Labels.Nodes.First(l => l.Name == triageLabel);
                        _triageLabelId = label.Id;
                    }

                    // triage label already applied, skip
                    continue;
                }
                else if (issue.Labels.PageInfo.HasNextPage)
                {
                    // TODO: Handle when issue has more than 100 labels
                    WriteGitHubActionsWarning("Unsupported scenario: issue " + issue.Number + " has more than 100 labels");
                }

                string? cutoffReason;
                if (!TryGetCutoffDate(issue.TimelineItems.Nodes, searchLabel, triageLabel, out DateTime? cutoffDate, out cutoffReason))
                {
                    (cutoffDate, cutoffReason) = await GetCutoffDateAsync(issue.Id, searchLabel, triageLabel);
                    if (cutoffDate == null)
                    {
                        WriteGitHubActionsWarning("Unsupported scenario: issue " + issue.Number + " did not find label in latest events");
                        continue;
                    }
                }

                int upvotes = GetPositiveReactionCount(issue, cutoffDate.Value);
                bool qualifies = upvotes >= upvoteCount;

                if (upvotes == -1)
                {
                    // Unsupported scenario already warned inside GetPositiveReactionCount
                    if (verbose)
                    {
                        Console.WriteLine($"Issue {issue.Url} - cutoff: {cutoffDate.Value:yyyy-MM-dd} ({cutoffReason}), upvotes: unknown (needs more data)");
                    }
                    continue;
                }

                if (verbose)
                {
                    Console.WriteLine($"Issue {issue.Url} - cutoff: {cutoffDate.Value:yyyy-MM-dd} ({cutoffReason}), upvotes: {upvotes}");
                }

                if (qualifies)
                {
                    Console.WriteLine($"Issue {issue.Url} has enough upvotes");
                    if (apply)
                    {
                        if (_triageLabelId == null)
                        {
                            _triageLabelId = await GetLabelIdAsync(config.Owner, config.Repo, triageLabel);
                        }

                        await AddLabelToIssueAsync(issue.Id, _triageLabelId);
                    }
                }
            }
        }

        private async IAsyncEnumerable<GetIssuesResult.IssuesModel> GetIssuesAsync(string owner, string repo, string label, int upvotes)
        {
            // See GitHub docs on resource/query limits. Increasing the counts has a multiplactive effect towards the hourly query limit.
            Dictionary<string, object?>? variables = new Dictionary<string, object?>()
            {
                ["owner"] = owner,
                ["repo"] = repo,
                ["after"] = null,
                ["label"] = label,
                ["timelineCount"] = 5, // adding icebox label is usually one of the most recent actions
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

        private static bool TryGetCutoffDate(IReadOnlyList<TimelineEvent>? events, string searchLabel, string triageLabel, [NotNullWhen(true)] out DateTime? cutoffDate, out string? cutoffReason)
        {
            if (events == null || events.Count == 0)
            {
                cutoffDate = null;
                cutoffReason = null;
                return false;
            }

            DateTime searchLabelAdded = events
                .Where(e => e.IsLabeledEvent && string.Equals(searchLabel, e.Label?.Name, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.CreatedAt)
                .MaxOrDefault();

            if (searchLabelAdded == default)
            {
                cutoffDate = null;
                cutoffReason = null;
                return false;
            }

            DateTime triageLabelRemoved = events
                .Where(e => e.IsUnlabeledEvent && string.Equals(triageLabel, e.Label?.Name, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.CreatedAt)
                .MaxOrDefault();

            if (triageLabelRemoved != default && triageLabelRemoved > searchLabelAdded)
            {
                cutoffDate = triageLabelRemoved;
                cutoffReason = "triage label removed";
            }
            else
            {
                cutoffDate = searchLabelAdded;
                cutoffReason = "search label added";
            }

            return true;
        }

        private async Task<(DateTime? cutoffDate, string? cutoffReason)> GetCutoffDateAsync(string issueId, string searchLabel, string triageLabel)
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
                return (null, null);
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
                return (null, null);
            }

            IReadOnlyList<TimelineEvent>? events = response.Data?.Node.TimelineItems.Nodes;
            if (!TryGetCutoffDate(events, searchLabel, triageLabel, out DateTime? cutoffDate, out string? cutoffReason))
            {
                return (null, null);
            }

            return (cutoffDate, cutoffReason);
        }

        private static int GetPositiveReactionCount(GetIssuesResult.IssuesModel issue, DateTime after)
        {
            if (!issue.Reactions.PageInfo.HasNextPage)
            {
                // No need to fetch more reactions, since we already have the complete list.
                return GetCustomerUpvoteCount(issue.Reactions.Nodes.Where(r => r.CreatedAt > after));
            }
            else
            {
                int count = GetCustomerUpvoteCount(issue.Reactions.Nodes.Where(r => r.CreatedAt > after));

                // If the oldest date we already have is more recent than the cutoff, then getting more reactions will not help
                DateTime? min = issue.Reactions.Nodes.Select(r => r.CreatedAt).MinOrDefault();
                if (min != null && min < after)
                {
                    return count;
                }

                // TODO: Need to get more reactions from GraphQL to check if upvote threshold met
                WriteGitHubActionsWarning("Unsupported scenario: issue " + issue?.Number + " needs to check more reactions for threshold check.");
                return -1;
            }

            static int GetCustomerUpvoteCount(IEnumerable<Reaction> reactions)
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

            static bool IsPositiveReaction(string? reaction)
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

                var configArgument = new Argument<string>("config");
                configArgument.Description = "Path to the JSON configuration file.";
                command.Add(configArgument);

                var applyOption = new Option<bool>("--apply");
                applyOption.Description = "Apply label changes to qualifying issues. Without this flag, the report runs in dry-run mode.";
                applyOption.SetDefaultValue(false);
                command.Add(applyOption);

                var verboseOption = new Option<bool>("--verbose");
                verboseOption.Description = "Output details for every issue, including cutoff date and upvote count.";
                verboseOption.SetDefaultValue(false);
                command.Add(verboseOption);

                command.SetHandler(async
                    (GitHubPat pat,
                    string configPath,
                    bool apply,
                    bool verbose) =>
                {
                    if (!File.Exists(configPath))
                    {
                        Console.Error.WriteLine("::error ::Config file not found: " + configPath);
                        return;
                    }

                    IceBoxConfig config;
                    try
                    {
                        config = IceBoxConfig.Load(configPath);
                    }
                    catch (JsonException ex)
                    {
                        Console.Error.WriteLine("::error ::Invalid config file: " + ex.Message);
                        return;
                    }

                    if (pat?.Value == null)
                    {
                        Console.Error.WriteLine("::error ::No GitHub access token available. Provide --pat, configure the GitHub CLI, or set up Git Credential Manager.");
                        return;
                    }

                    var serviceProvider = new ServiceCollection()
                        .AddGithubIssueTagger(pat)
                        .BuildServiceProvider();

                    var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
                    using (scopeFactory.CreateScope())
                    {
                        var report = serviceProvider.GetRequiredService<IceBoxReport>();
                        await report.RunAsync(config, apply, verbose);
                    }
                }, patBinder, configArgument, applyOption, verboseOption);

                return command;
            }
        }
    }
}
