using System.Collections.Generic;

namespace GithubIssueTagger.Reports.CiReliability
{
    internal class ReportData
    {
        public string? SprintName { get; init; }

        public string? KustoQuery { get; init; }

        public IReadOnlyList<FailedBuild>? FailedBuilds { get; init; }

        public IReadOnlyDictionary<string, string>? TrackingIssues { get; init; }

        public int TotalBuilds { get; init; }

        internal struct FailedBuild
        {
            public long Id { get; init; }
            public string? Number { get; init; }
            public IReadOnlyList<FailureDetail>? Details { get; init; }
        }

        internal class FailureDetail
        {
            public string? Job { get; init; }
            public string? Task { get; init; }
            public string? Details { get; init; }
        }
    }
}
