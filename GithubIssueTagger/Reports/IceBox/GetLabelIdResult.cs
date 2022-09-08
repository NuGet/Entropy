using GithubIssueTagger.Reports.IceBox.Models;
using System;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.Reports.IceBox
{
    internal class GetLabelIdResult
    {
        public GetLabelIdResult(_Repository repository)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [JsonPropertyName("repository")]
        public _Repository Repository { get; init; }

        internal class _Repository
        {
            public _Repository(Label label)
            {
                Label = label ?? throw new ArgumentNullException(nameof(label));
            }

            [JsonPropertyName("label")]
            public Label Label { get; init; }
        }
    }
}
