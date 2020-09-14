using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace find_incomplete_tests.Model
{
    internal record Build
    {
        [JsonPropertyName("_links")]
        public IReadOnlyDictionary<string, Link> links { get; init; }
        public IReadOnlyList<ValidationResult> validationResults { get; init; }
        public uint id { get; init; }
        public string buildNumber { get; init; }
        public string status { get; init; }
        public string result { get; init; }
        public DateTime queueTime { get; init; }
        public DateTime startTime { get; init; }
        public DateTime finishTime { get; init; }
        public string url { get; init; }
        public string sourceBranch { get; init; }
        public string sourceVersion { get; init; }

        internal record Link
        {
            public string href { get; init; }
        }

        internal record ValidationResult
        {
            public string result { get; init; }
            public string message { get; init; }
        }
    }
}
