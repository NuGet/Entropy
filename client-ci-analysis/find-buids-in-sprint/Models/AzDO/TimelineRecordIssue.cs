namespace find_buids_in_sprint.Models.AzDO
{
    internal class TimelineRecordIssue
    {
        public string @type { get; set; }
        public string category { get; set; }
        public string message { get; set; }
        public Data data { get; set; }

        internal class Data
        {
            public string @type { get; set; }
            public string sourcepath { get; set; }
            public string linenumber { get; set; }
            public string columnnumber { get; set; }
            public string code { get; set; }
            public string logFileLineNumber { get; set; }
        }
    }
}