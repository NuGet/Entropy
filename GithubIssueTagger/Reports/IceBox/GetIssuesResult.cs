using GithubIssueTagger.GraphQL;
using GithubIssueTagger.Reports.IceBox.Models;
using System;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.Reports.IceBox
{
    internal class GetIssuesResult
    {
        public GetIssuesResult(RepositoryModel repository)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [JsonPropertyName("repository")]
        public RepositoryModel Repository { get; init; }

        internal class RepositoryModel
        {
            public RepositoryModel(Connection<IssuesModel> issues)
            {
                Issues = issues ?? throw new ArgumentNullException(nameof(issues));
            }

            [JsonPropertyName("issues")]
            public Connection<IssuesModel> Issues { get; init; }
        }

        internal class IssuesModel
        {
            public IssuesModel(string id, int? number, string title, string url, Connection<TimelineEvent> timelineItems, Connection<Reaction> reactions, Connection<Label> labels)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                Number = number ?? throw new ArgumentNullException(nameof(number));
                Title = title ?? throw new ArgumentNullException(nameof(title));
                Url = url ?? throw new ArgumentNullException(nameof(url));
                TimelineItems = timelineItems ?? throw new ArgumentNullException(nameof(timelineItems));
                Reactions = reactions ?? throw new ArgumentNullException(nameof(reactions));
                Labels = labels ?? throw new ArgumentNullException(nameof(labels));
            }

            [JsonPropertyName("id")]
            public string Id { get; init; }

            [JsonPropertyName("number")]
            public int? Number { get; init; }

            [JsonPropertyName("title")]
            public string Title { get; init; }

            [JsonPropertyName("url")]
            public string Url { get; init; }

            [JsonPropertyName("timelineItems")]
            public Connection<TimelineEvent> TimelineItems { get; init; }

            [JsonPropertyName("reactions")]
            public Connection<Reaction> Reactions { get; init; }

            [JsonPropertyName("labels")]
            public Connection<Label> Labels { get; init; }
        }
    }
}
