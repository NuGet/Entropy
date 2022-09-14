﻿using System.Diagnostics;
using System.Text.Json.Serialization;

namespace NuGet.GithubEventHandler.Model.GitHub
{
    // OctoKit doesn't provide a model for webhooks, and their other models don't play nice with System.Text.Json.
    // Hence, we need to define our own models.
    [DebuggerDisplay("{Login}")]
    public class User
    {
        [JsonPropertyName("login")]
        public string? Login { get; init; }
    }
}
