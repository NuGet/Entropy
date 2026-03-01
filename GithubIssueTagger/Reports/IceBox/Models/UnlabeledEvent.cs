using System;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.Reports.IceBox.Models
{
    internal class UnlabeledEvent
    {
        public UnlabeledEvent(Label label, DateTime createdAt)
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
