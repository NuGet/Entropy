using GithubIssueTagger.GraphQL;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports.IceBox
{
    [CommandFactory(typeof(IceBoxReportCommandFactory))]
    internal class IceBoxReport : IReport
    {
        private readonly GitHubGraphQLClient _client;

        public IceBoxReport(GitHubGraphQLClient client)
        {
            _client = client;
        }

        public Task RunAsync()
        {
            Console.WriteLine("IceBox is not available in interactive mode");
            return Task.CompletedTask;
        }

        public async Task RunAsync(string owner, string repo, string label, int upvoteCount)
        {
            await foreach (GetIssues.IssuesModel issue in GetIssuesAsync(owner, repo, label, upvoteCount))
            {
                if (!TryGetLastLabelTime(issue, label, out DateTime? labelAdded))
                {
                    // TODO: If we reach here, we need to do a different GraphQL query to get more events for this issue to find the last time the label was added.
                    Console.WriteLine("##vso[task.logissue type=warning]Unsupported scenario: issue " + issue.Number + " did not find label in latest events");
                    continue;
                }

                if (HasEnoughPositiveReactions(issue, labelAdded.Value, upvoteCount))
                {
                    Console.WriteLine($"Issue {issue.Number} has enough upvotes");
                }
            }
        }

        private async IAsyncEnumerable<GetIssues.IssuesModel> GetIssuesAsync(string owner, string repo, string label, int upvotes)
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
                GraphQLResponse<GetIssues>? response = await _client.SendAsync<GetIssues>(request);
                if (response?.Data?.Repository.Issues.Nodes != null)
                {
                    foreach (GetIssues.IssuesModel issue in response.Data.Repository.Issues.Nodes)
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

        private bool TryGetLastLabelTime(GetIssues.IssuesModel issue, string label, [NotNullWhen(true)] out DateTime? labelAdded)
        {
            IEnumerable<DateTime>? enumerable = issue?.TimelineItems?.Nodes
                ?.Where(e => string.Equals(label, e?.Label?.Name, StringComparison.OrdinalIgnoreCase))
                ?.Select(e => e.CreatedAt);
            if (enumerable == null || !enumerable.Any())
            {
                labelAdded = null;
                return false;
            }
            else
            {
                labelAdded = enumerable.Max();
                return true;
            }
        }

        private bool HasEnoughPositiveReactions(GetIssues.IssuesModel issue, DateTime after, int upvotes)
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
                    Console.WriteLine("##vso[task.logissue type=warning]Unsupported scenario: issue " + issue?.Number + " needs to check more reactions for theshold check.");
                    return false;
                }
            }

            static int GetCustomerUpvoteCount(IEnumerable<GetIssues.Reaction> reactions)
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

                command.SetHandler(async
                    (GitHubPat pat,
                    string owner,
                    string repo, 
                    string label, 
                    int upvotes) =>
                {
                    var serviceProvider = new ServiceCollection()
                        .AddGithubIssueTagger(pat)
                        .BuildServiceProvider();

                    var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
                    using (scopeFactory.CreateScope())
                    {
                        var report = serviceProvider.GetRequiredService<IceBoxReport>();
                        await report.RunAsync(owner, repo, label, upvotes);
                    }
                }, patBinder, ownerOption, repoOption, labelOption, upvotesOption);

                return command;
            }
        }
    }
}
