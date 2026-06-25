using GithubIssueTagger.GraphQL;
using GithubIssueTagger.Reports.IceBox.Models;
using System;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.Reports.IceBox
{
    internal class IssuesModel
    {
        public IssuesModel(string id, int? number, string title, bool closed, Connection<TimelineEvent> timelineItems, Connection<Reaction> reactions, Connection<Label> labels)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Number = number ?? throw new ArgumentNullException(nameof(number));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Closed = closed;
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

        [JsonPropertyName("closed")]
        public bool Closed { get; init; }

        [JsonPropertyName("timelineItems")]
        public Connection<TimelineEvent> TimelineItems { get; init; }

        [JsonPropertyName("reactions")]
        public Connection<Reaction> Reactions { get; init; }

        [JsonPropertyName("labels")]
        public Connection<Label> Labels { get; init; }
    }
}
