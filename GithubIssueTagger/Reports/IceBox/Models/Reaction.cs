using System;
using System.Text.Json.Serialization;

namespace GithubIssueTagger.Reports.IceBox.Models
{
    internal class Reaction
    {
        public Reaction(UserModel user, string content, DateTime createdAt)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            CreatedAt = createdAt;
        }

        [JsonPropertyName("user")]
        public UserModel User { get; init; }

        [JsonPropertyName("content")]
        public string Content { get; init; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; init; }

        internal class UserModel
        {
            public UserModel(string login)
            {
                Login = login ?? throw new ArgumentNullException(nameof(login));
            }

            [JsonPropertyName("login")]
            public string Login { get; init; }
        }
    }
}
