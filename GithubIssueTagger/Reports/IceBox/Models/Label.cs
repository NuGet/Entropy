using System.Text.Json.Serialization;
using System;

namespace GithubIssueTagger.Reports.IceBox.Models
{
    internal class Label
    {
        public Label(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        [JsonPropertyName("name")]
        public string Name { get; init; }
    }
}
