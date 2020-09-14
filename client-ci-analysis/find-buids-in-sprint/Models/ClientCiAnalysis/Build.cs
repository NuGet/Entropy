using System;
using System.Collections.Generic;

namespace find_buids_in_sprint.Models.ClientCiAnalysis
{
    internal class Build
    {
        public uint id { get; set; }
        public string buildNumber { get; set; }
        public string url { get; set; }
        public string result { get; set; }
        public DateTime finishTime { get; set; }
        public Dictionary<string, List<Attempt>> jobs { get; set; }
    }
}
