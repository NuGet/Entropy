using System.Diagnostics;
using System.Text.Json.Serialization;

namespace NuGet.GithubEventHandler.Model.GitHub
{
    // OctoKit doesn't provide a model for webhooks, and their other models don't play nice with System.Text.Json.
    // Hence, we need to define our own models.
    [DebuggerDisplay("{FullName}")]
    public class Repository
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("full_name")]
        public string? FullName { get; init; }

        [JsonPropertyName("owner")]
        public User? Owner { get; init; }
    }
}
