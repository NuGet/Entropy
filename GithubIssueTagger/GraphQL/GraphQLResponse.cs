using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.GraphQL
{
    internal class GraphQLResponse<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; init; }

        [JsonPropertyName("errors")]
        public IReadOnlyList<GraphQLResponseError>? Errors { get; init; }
    }
}
