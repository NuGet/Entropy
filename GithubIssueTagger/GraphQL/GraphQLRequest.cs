using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.GraphQL
{
    internal class GraphQLRequest
    {
        public GraphQLRequest(string query)
        {
            Query = query;
        }

        [JsonPropertyName("query")]
        public string Query { get; init; }

        [JsonPropertyName("variables")]
        public IReadOnlyDictionary<string, object?>? Variables { get; init; }
    }
}
