using System;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.Reports.IceBox.Models
{
    internal class TimelineEvent
    {
        public TimelineEvent(string typeName, Label label, DateTime createdAt)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            Label = label ?? throw new ArgumentNullException(nameof(label));
            CreatedAt = createdAt;
        }

        [JsonPropertyName("__typename")]
        public string TypeName { get; init; }

        [JsonPropertyName("label")]
        public Label Label { get; init; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; init; }

        public bool IsLabeledEvent => string.Equals(TypeName, "LabeledEvent", StringComparison.OrdinalIgnoreCase);
        public bool IsUnlabeledEvent => string.Equals(TypeName, "UnlabeledEvent", StringComparison.OrdinalIgnoreCase);
    }
}
