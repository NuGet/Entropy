using GithubIssueTagger.GraphQL;
using System;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.Reports.IceBox
{
    internal class GetIssueNumbersResult
    {
        public GetIssueNumbersResult(RepositoryModel repository)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [JsonPropertyName("repository")]
        public RepositoryModel Repository { get; init; }

        internal class RepositoryModel
        {
            public RepositoryModel(Connection<IssueNumberModel> issues)
            {
                Issues = issues ?? throw new ArgumentNullException(nameof(issues));
            }

            [JsonPropertyName("issues")]
            public Connection<IssueNumberModel> Issues { get; init; }
        }

        internal class IssueNumberModel
        {
            public IssueNumberModel(int? number)
            {
                Number = number ?? throw new ArgumentNullException(nameof(number));
            }

            [JsonPropertyName("number")]
            public int? Number { get; init; }
        }
    }
}
