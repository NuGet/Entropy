using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.Reports.IceBox
{
    internal class IceBoxConfig
    {
        [JsonPropertyName("owner")]
        public required string Owner { get; init; }

        [JsonPropertyName("repo")]
        public required string Repo { get; init; }

        [JsonPropertyName("searchLabel")]
        public required string SearchLabel { get; init; }

        [JsonPropertyName("triage")]
        public required TriageConfig Triage { get; init; }

        internal class TriageConfig
        {
            [JsonPropertyName("upvotes")]
            public required int Upvotes { get; init; }

            [JsonPropertyName("label")]
            public required string Label { get; init; }
        }

        public static IceBoxConfig Load(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<IceBoxConfig>(json)
                ?? throw new JsonException("Failed to deserialize config file: " + path);
        }
    }
}
