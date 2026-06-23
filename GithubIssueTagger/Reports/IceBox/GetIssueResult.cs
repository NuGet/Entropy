using System;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.Reports.IceBox
{
    internal class GetIssueResult
    {
        public GetIssueResult(RepositoryModel repository)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [JsonPropertyName("repository")]
        public RepositoryModel Repository { get; init; }

        internal class RepositoryModel
        {
            public RepositoryModel(GetIssuesResult.IssuesModel? issue)
            {
                Issue = issue;
            }

            [JsonPropertyName("issue")]
            public GetIssuesResult.IssuesModel? Issue { get; init; }
        }
    }
}
