using System.Diagnostics;
using System.Text.Json.Serialization;

namespace NuGet.GithubEventHandler.Model.GitHub
{
    // OctoKit doesn't provide a model for webhooks, and their other models don't play nice with System.Text.Json.
    // Hence, we need to define our own models.
    [DebuggerDisplay("{Action} - {Repository.FullName}")]
    public class WebhookPayload
    {
        [JsonPropertyName("action")]
        public string? Action { get; init; }

        [JsonPropertyName("number")]
        public int? Number { get; init; }

        [JsonPropertyName("pull_request")]
        public PullRequest? PullRequest { get; init; }

        [JsonPropertyName("label")]
        public Label? Label { get; init; }

        [JsonPropertyName("repository")]
        public Repository? Repository { get; init; }
    }
}
