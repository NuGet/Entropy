using GithubIssueTagger.GraphQL;
using System;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.Reports.IceBox
{
    internal class GetIssues
    {
        public GetIssues(RepositoryModel repository)
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
            public IssuesModel(string id, int? number, string title, Connection<LabeledEvent> timelineItems, Connection<Reaction> reactions)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                Number = number ?? throw new ArgumentNullException(nameof(number));
                Title = title ?? throw new ArgumentNullException(nameof(title));
                TimelineItems = timelineItems ?? throw new ArgumentNullException(nameof(timelineItems));
                Reactions = reactions ?? throw new ArgumentNullException(nameof(reactions));
            }

            [JsonPropertyName("id")]
            public string? Id { get; init; }

            [JsonPropertyName("number")]
            public int? Number { get; init; }

            [JsonPropertyName("title")]
            public string? Title { get; init; }

            [JsonPropertyName("timelineItems")]
            public Connection<LabeledEvent> TimelineItems { get; init; }

            [JsonPropertyName("reactions")]
            public Connection<Reaction> Reactions { get; init; }
        }

        internal class LabeledEvent
        {
            public LabeledEvent(LabelModel label, DateTime createdAt)
            {
                Label = label ?? throw new ArgumentNullException(nameof(label));
                CreatedAt = createdAt;
            }

            [JsonPropertyName("label")]
            public LabelModel Label { get; init; }

            [JsonPropertyName("createdAt")]
            public DateTime CreatedAt { get; init; }

            internal class LabelModel
            {
                public LabelModel(string name)
                {
                    Name = name ?? throw new ArgumentNullException(nameof(name));
                }

                [JsonPropertyName("name")]
                public string Name { get; init; }
            }
        }

        internal class Reaction
        {
            public Reaction(UserModel user, string content, DateTime createdAt)
            {
                User = user ?? throw new ArgumentNullException(nameof(user));
                Content = content ?? throw new ArgumentNullException(nameof(content));
                CreatedAt = createdAt;
            }

            [JsonPropertyName("user")]
            public UserModel User { get; init; }

            [JsonPropertyName("content")]
            public string Content { get; init; }

            [JsonPropertyName("createdAt")]
            public DateTime CreatedAt { get; init; }

            internal class UserModel
            {
                public UserModel(string login)
                {
                    Login = login ?? throw new ArgumentNullException(nameof(login));
                }

                [JsonPropertyName("login")]
                public string Login { get; init; }
            }
        }
    }
}
