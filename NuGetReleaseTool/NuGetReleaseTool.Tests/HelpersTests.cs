using System.Net;
using System.Text;
using System.Text.Json;
using Octokit;
using Octokit.Internal;
using Xunit;

namespace NuGetReleaseTool.Tests
{
    public class HelpersTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Related: https://github.com/NuGet/Home/issues/123")]
        public async Task GetCommitDetails_IncludesDevelopmentLinksAcrossPages(string? body)
        {
            using var handler = new GitHubHandler(body,
                LinkedIssuesPage(true, "next-page",
                    "https://github.com/NuGet/Home/issues/123",
                    "https://github.com/NuGet/Home/issues/1000",
                    "https://github.com/Other/Repo/issues/456"),
                LinkedIssuesPage(false, null,
                    "https://github.com/NuGet/Home/issues/123",
                    "https://github.com/NuGet/Client.Engineering/issues/456"));
            var client = CreateClient(handler);

            var commits = await Helpers.GetCommitDetails(client, "NuGet", "NuGet.Client",
                new[] { "NUGET/HOME;NuGet/Client.Engineering" }, new[] { CreateCommit("Fix something (#42)") });

            var commit = Assert.Single(commits);
            Assert.Equal(
                new[] { "https://github.com/nuget/client.engineering/issues/456", "https://github.com/nuget/home/issues/123" },
                commit.Issues.Select(issue => issue.Item2).OrderBy(url => url));
            Assert.Equal(new[] { 123, 456 }, commit.Issues.Select(issue => issue.Item1).OrderBy(id => id));
            Assert.Equal(2, handler.GraphQLRequests.Count);
            var firstVariables = handler.GraphQLRequests[0].GetProperty("variables");
            Assert.Equal("NuGet", firstVariables.GetProperty("owner").GetString());
            Assert.Equal("NuGet.Client", firstVariables.GetProperty("name").GetString());
            Assert.Equal(42, firstVariables.GetProperty("number").GetInt32());
            Assert.Equal("next-page", handler.GraphQLRequests[1].GetProperty("variables").GetProperty("after").GetString());
        }

        [Fact]
        public async Task GetCommitDetails_AnonymousClientRetainsTextLinksWithoutGraphQL()
        {
            using var handler = new GitHubHandler("Related: https://github.com/NuGet/Home/issues/123");
            var client = CreateClient(handler, authenticated: false);

            var commits = await Helpers.GetCommitDetails(client, "NuGet", "NuGet.Client",
                new[] { "NuGet/Home" }, new[] { CreateCommit("Fix something (#42)") });

            var issue = Assert.Single(Assert.Single(commits).Issues);
            Assert.Equal(123, issue.Item1);
            Assert.Empty(handler.GraphQLRequests);
        }

        [Theory]
        [InlineData("Update SPDX licenses (#42)")]
        [InlineData("Commit without a pull request")]
        public async Task GetCommitDetails_DoesNotQueryLinksForSkippedCommits(string message)
        {
            using var handler = new GitHubHandler("https://github.com/NuGet/Home/issues/123");
            var client = CreateClient(handler);

            var commits = await Helpers.GetCommitDetails(client, "NuGet", "NuGet.Client",
                new[] { "NuGet/Home" }, new[] { CreateCommit(message) });

            Assert.Empty(Assert.Single(commits).Issues);
            Assert.Empty(handler.GraphQLRequests);
        }

        [Theory]
        [InlineData("""{"errors":[{"message":"Access denied"}],"data":null}""", "Access denied")]
        [InlineData("""{"data":{"repository":{"pullRequest":null}}}""", "NuGet/NuGet.Client#42")]
        public async Task GetCommitDetails_ReportsGraphQLErrors(string response, string expectedMessage)
        {
            using var handler = new GitHubHandler("", response);
            var client = CreateClient(handler);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Helpers.GetCommitDetails(client, "NuGet", "NuGet.Client",
                    new[] { "NuGet/Home" }, new[] { CreateCommit("Fix something (#42)") }));

            Assert.Contains(expectedMessage, exception.Message);
        }

        private static GitHubClient CreateClient(GitHubHandler handler, bool authenticated = true)
        {
            var connection = new Connection(new ProductHeaderValue("NuGetReleaseTool.Tests"),
                new HttpClientAdapter(() => handler));
            return new GitHubClient(connection)
            {
                Credentials = authenticated ? new Credentials("test-token") : Credentials.Anonymous
            };
        }

        private static GitHubCommit CreateCommit(string message)
        {
            return new SimpleJsonSerializer().Deserialize<GitHubCommit>(
                JsonSerializer.Serialize(new { sha = "abc123", commit = new { message } }));
        }

        private static string LinkedIssuesPage(bool hasNextPage, string? endCursor, params string[] urls)
        {
            return JsonSerializer.Serialize(new
            {
                data = new
                {
                    repository = new
                    {
                        pullRequest = new
                        {
                            closingIssuesReferences = new
                            {
                                nodes = urls.Select(url => new { url }),
                                pageInfo = new { hasNextPage, endCursor }
                            }
                        }
                    }
                }
            });
        }

        private sealed class GitHubHandler : HttpMessageHandler
        {
            private readonly string? _body;
            private readonly Queue<string> _graphQLResponses;
            public List<JsonElement> GraphQLRequests { get; } = new();

            public GitHubHandler(string? body, params string[] graphQLResponses)
            {
                _body = body;
                _graphQLResponses = new Queue<string>(graphQLResponses);
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string response;
                if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == "/repos/NuGet/NuGet.Client/pulls/42")
                {
                    response = JsonSerializer.Serialize(new { number = 42, body = _body });
                }
                else
                {
                    Assert.Equal(HttpMethod.Post, request.Method);
                    Assert.Equal("https://api.github.com/graphql", request.RequestUri?.AbsoluteUri);
                    Assert.Equal("test-token", request.Headers.Authorization?.Parameter);
                    Assert.NotNull(request.Content);
                    using var document = JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellationToken));
                    GraphQLRequests.Add(document.RootElement.Clone());
                    response = _graphQLResponses.Dequeue();
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(response, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
