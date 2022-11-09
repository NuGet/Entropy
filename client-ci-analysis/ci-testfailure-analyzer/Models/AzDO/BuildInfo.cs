using System;
using System.Diagnostics;

namespace ci_testfailure_analyzer.Models.AzDO
{
    [DebuggerDisplay("{buildNumber} ({id})")]
    internal class BuildInfo
    {
        public uint id { get; set; }
        public string buildNumber { get; set; }
        public string status { get; set; }
        public string result { get; set; }
        public DateTime queueTime { get; set; }
        public DateTime startTime { get; set; }
        public DateTime finishTime { get; set; }
    }
}
