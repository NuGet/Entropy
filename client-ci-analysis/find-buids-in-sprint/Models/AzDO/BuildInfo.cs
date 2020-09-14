using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace find_buids_in_sprint.Models.AzDO
{
    [DebuggerDisplay("{buildNumber} ({id})")]
    internal class BuildInfo
    {
        public Definition definition { get; set; }
        public uint id { get; set; }
        public string buildNumber { get; set; }
        public string status { get; set; }
        public string result { get; set; }
        public DateTime queueTime { get; set; }
        public DateTime startTime { get; set; }
        public DateTime finishTime { get; set; }

        [JsonPropertyName("_links")]
        public Dictionary<string, Link> links { get; set; }

        public string url { get; set; }

        public List<BuildInfoValidationResult> validationResults { get; set; }

        public string sourceBranch { get; set; }
    }
}
