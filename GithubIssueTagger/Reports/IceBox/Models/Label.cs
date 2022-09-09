using System.Text.Json.Serialization;
using System;

namespace GithubIssueTagger.Reports.IceBox.Models
{
    internal class Label
    {
        public Label(string name, string id)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        [JsonPropertyName("name")]
        public string Name { get; init; }

        [JsonPropertyName("id")]
        public string Id { get; init; }
    }
}
