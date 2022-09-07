using System.Text.Json.Serialization;
using System;

namespace GithubIssueTagger.Reports.IceBox.Models
{
    internal class LabeledEvent
    {
        public LabeledEvent(Label label, DateTime createdAt)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            CreatedAt = createdAt;
        }

        [JsonPropertyName("label")]
        public Label Label { get; init; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; init; }
    }
}
