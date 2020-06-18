﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace find_buids_in_sprint.Models
{
    public class BuildInfo
    {
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
    }
}
