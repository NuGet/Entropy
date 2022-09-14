using System.Diagnostics;
using System.Text.Json.Serialization;

namespace NuGet.GithubEventHandler.Model.GitHub
{
    // OctoKit doesn't provide a model for webhooks, and their other models don't play nice with System.Text.Json.
    // Hence, we need to define our own models.
    [DebuggerDisplay("{Name}")]
    public class Label
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}
