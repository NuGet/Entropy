using System.Collections.Generic;

namespace find_incomplete_tests.Model
{
    internal record Timeline
    {
        public IReadOnlyList<Record> records { get; init; }

        internal record Record
        {
            public string id { get; init; }
            public string parentId { get; init; }
            public string type { get; init; }
            public string name { get; init; }
            public string state { get; init; }
            public string result { get; init; }
            public int order { get; init; }
            public uint errorCount { get; init; }
            public uint warningCount { get; init; }
            public Log log { get; init; }
            public Task task { get; init; }
            public uint attempt { get; init; }
            public IReadOnlyList<Issue> issues { get; init; }
        }

        internal record Log
        {
            public uint id { get; init; }
            public string type { get; init; }
            public string url { get; init; }
        }

        internal record Task
        {
            public string id { get; init; }
            public string name { get; init; }
            public string version { get; init; }
        }

        internal record Issue
        {
            public string type { get; init; }
            public string category { get; init; }
            public string message { get; init; }
            public IReadOnlyDictionary<string, string> data { get; init; }
        }
    }
}
