using System;
using System.Collections.Generic;

namespace find_buids_in_sprint.Models.AzDO
{
    internal class TimelineRecord
    {
        public List<TimelinePreviousAttempt> previousAttempts { get; set; }
        public string id { get; set; }
        public string parentId { get; set; }
        public string @type { get; set; }
        public string name { get; set; }
        public DateTime? startTime { get; set; }
        public DateTime? finishTime { get; set; }
        public string state { get; set; }
        public string result { get; set; }
        public string workerName { get; set; }
        public int order { get; set; }
        public TimelineRecordLog log { get; set; }
        public int attempt { get; set; }
        public List<TimelineRecordIssue> issues { get; set; }
    }
}