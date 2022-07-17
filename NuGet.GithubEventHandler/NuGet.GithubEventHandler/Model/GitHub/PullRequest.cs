using System.Diagnostics;
using System.Text.Json.Serialization;

namespace NuGet.GithubEventHandler.Model.GitHub
{
    // OctoKit doesn't provide a model for webhooks, and their other models don't play nice with System.Text.Json.
    // Hence, we need to define our own models.
    [DebuggerDisplay("({Number}) {Title}")]
    public class PullRequest
    {
        [JsonPropertyName("number")]
        public int? Number { get; init; }

        [JsonPropertyName("state")]
        public string? State { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }
    }
}
