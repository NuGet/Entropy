using System;
using System.Text.Json.Serialization;
using GithubIssueTagger.GraphQL;
using GithubIssueTagger.Reports.IceBox.Models;

namespace GithubIssueTagger.Reports.IceBox
{
    internal class GetLabeledEventsResult
    {
        public GetLabeledEventsResult(Issue node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }

        [JsonPropertyName("node")]
        public Issue Node { get; init; }

        internal class Issue
        {
            public Issue(Connection<TimelineEvent> timelineItems)
            {
                TimelineItems = timelineItems ?? throw new ArgumentNullException(nameof(timelineItems));
            }

            [JsonPropertyName("timelineItems")]
            public Connection<TimelineEvent> TimelineItems { get; init; }
        }
    }
}
