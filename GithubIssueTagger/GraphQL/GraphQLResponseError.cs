using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.GraphQL
{
    internal class GraphQLResponseError
    {
        [JsonPropertyName("extensions")]
        public ExtensionsModel? Extensions { get; init; }

        [JsonPropertyName("locations")]
        public IReadOnlyList<Location>? Locations { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }

        internal class ExtensionsModel
        {
            [JsonPropertyName("value")]
            public object? Value { get; init; }

            [JsonPropertyName("problems")]
            public IReadOnlyList<Problem>? Problems { get; init; }
        }

        internal class Problem
        {
            [JsonPropertyName("path")]
            public IReadOnlyList<string>? Path { get; init; }

            [JsonPropertyName("explanation")]
            public string? Explanation { get; init; }
        }

        internal class Location
        {
            [JsonPropertyName("line")]
            public int? Line { get; init; }

            [JsonPropertyName("column")]
            public int? Column { get; init; }
        }
    }
}