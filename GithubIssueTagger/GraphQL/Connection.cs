using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.GraphQL
{
    internal class Connection<T>
    {
        public Connection(int totalCount, PageInfoModel pageInfo, IReadOnlyList<T> nodes)
        {
            TotalCount = totalCount;
            PageInfo = pageInfo ?? throw new ArgumentNullException(nameof(pageInfo));
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; init; }

        [JsonPropertyName("pageInfo")]
        public PageInfoModel PageInfo { get; init; }

        [JsonPropertyName("nodes")]
        public IReadOnlyList<T> Nodes { get; init; }
    }

    internal class PageInfoModel
    {
        public PageInfoModel(bool hasNextPage, string? endCursor)
        {
            HasNextPage = hasNextPage;
            EndCursor = endCursor;
        }

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; init; }

        [JsonPropertyName("endCursor")]
        public string? EndCursor { get; init; }
    }
}
