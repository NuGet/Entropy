using System.Collections.Generic;
using System.Threading.Tasks;
using GithubIssueTagger.GraphQL;
using GithubIssueTagger.Reports.IceBox.Models;

namespace GithubIssueTagger.Reports.IceBox
{
    /// <summary>
    /// Wraps the GraphQL queries the IceBox report needs, keeping the report itself focused on policy.
    /// </summary>
    /// <remarks>
    /// Issues are fetched one at a time via <see cref="GetIssueAsync"/> rather than in bulk because
    /// GitHub's GraphQL API mis-evaluates nested connections (notably <c>timelineItems</c> with an
    /// <c>itemTypes</c> filter) when they are nested under the <c>issues</c> list connection, returning a
    /// tiny, oldest slice instead of the requested window. The bulk <see cref="GetIssueNumbersAsync"/>
    /// query therefore only fetches the matching issue numbers.
    /// </remarks>
    internal class IceBoxGitHubClient
    {
        private readonly GitHubGraphQLClient _client;

        public IceBoxGitHubClient(GitHubGraphQLClient client)
        {
            _client = client;
        }

        /// <summary>Returns the numbers of every open issue that has the given label.</summary>
        public async Task<IReadOnlyList<int>> GetIssueNumbersAsync(string owner, string repo, string label)
        {
            var numbers = new List<int>();

            Dictionary<string, object?>? variables = new Dictionary<string, object?>()
            {
                ["owner"] = owner,
                ["repo"] = repo,
                ["after"] = null,
                ["label"] = label
            };

            var request = new GraphQLRequest(IceBoxResource.GetIssues)
            {
                Variables = variables
            };

            while (variables != null)
            {
                GraphQLResponse<GetIssueNumbersResult>? response = await _client.SendAsync<GetIssueNumbersResult>(request);

                if (response?.Errors?.Count > 0)
                {
                    GitHubActionsLog.GraphQlErrors(response.Errors);
                }

                Connection<GetIssueNumbersResult.IssueNumberModel>? issues = response?.Data?.Repository.Issues;
                if (issues?.Nodes != null)
                {
                    foreach (GetIssueNumbersResult.IssueNumberModel node in issues.Nodes)
                    {
                        if (node.Number != null)
                        {
                            numbers.Add(node.Number.Value);
                        }
                    }
                }

                PageInfoModel? pageInfo = issues?.PageInfo;
                if (pageInfo?.HasNextPage == true && pageInfo.EndCursor != null)
                {
                    variables["after"] = pageInfo.EndCursor;
                }
                else
                {
                    variables = null;
                }
            }

            return numbers;
        }

        /// <summary>
        /// Fetches a single issue with its recent timeline events, reactions and labels. Returns
        /// <see langword="null"/> when the issue cannot be found.
        /// </summary>
        public async Task<IssuesModel?> GetIssueAsync(string owner, string repo, int number, int timelineCount, int reactionCount)
        {
            var variables = new Dictionary<string, object?>()
            {
                ["owner"] = owner,
                ["repo"] = repo,
                ["number"] = number,
                ["timelineCount"] = timelineCount,
                ["reactionCount"] = reactionCount
            };

            var request = new GraphQLRequest(IceBoxResource.GetIssueByNumber)
            {
                Variables = variables
            };

            GraphQLResponse<GetIssueResult>? response = await _client.SendAsync<GetIssueResult>(request);

            if (response?.Errors?.Count > 0)
            {
                GitHubActionsLog.GraphQlErrors(response.Errors);
            }

            return response?.Data?.Repository.Issue;
        }

        /// <summary>
        /// Fetches a larger window of labeled/unlabeled timeline events for an issue, used when the
        /// search label add is older than the window returned by <see cref="GetIssueAsync"/>.
        /// </summary>
        public async Task<IReadOnlyList<TimelineEvent>?> GetTimelineEventsAsync(string issueId)
        {
            var variables = new Dictionary<string, object?>()
            {
                ["issue"] = issueId
            };

            var request = new GraphQLRequest(IceBoxResource.GetLabeledEvents)
            {
                Variables = variables
            };

            GraphQLResponse<GetLabeledEventsResult>? response = await _client.SendAsync<GetLabeledEventsResult>(request);

            if (response?.Errors?.Count > 0)
            {
                GitHubActionsLog.GraphQlErrors(response.Errors);
            }

            return response?.Data?.Node.TimelineItems.Nodes;
        }

        /// <summary>Resolves the node id of a label by name, or <see langword="null"/> when not found.</summary>
        public async Task<string?> GetLabelIdAsync(string owner, string repo, string label)
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
                GitHubActionsLog.GraphQlErrors(response.Errors);
            }

            string? id = response?.Data?.Repository?.Label?.Id;

            if (id == null)
            {
                GitHubActionsLog.Warning("Unsupported scenario: GetLabelIdAsync failed.");
            }

            return id;
        }

        /// <summary>Adds the label with the given node id to the issue with the given node id.</summary>
        public async Task AddLabelToIssueAsync(string issueId, string? labelId)
        {
            var variables = new Dictionary<string, object?>()
            {
                ["issue"] = issueId,
                ["label"] = labelId
            };

            var request = new GraphQLRequest(IceBoxResource.AddLabelToIssue)
            {
                Variables = variables
            };

            GraphQLResponse<object>? response = await _client.SendAsync<object>(request);

            if (response?.Errors?.Count > 0)
            {
                GitHubActionsLog.Warning("Unsupported scenario: AddLabelToIssue failed.");
                GitHubActionsLog.GraphQlErrors(response.Errors);
            }
        }
    }
}
