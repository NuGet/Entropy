using System.Collections.Generic;

namespace GithubIssueTagger.Reports.CiReliability
{
    internal class ReportData
    {
        public string? SprintName { get; init; }

        public string? QueryName { get; init; }

        public string? KustoQuery { get; init; }

        public required IReadOnlyList<FailedBuild> FailedBuilds { get; init; }

        public required IReadOnlyDictionary<string, string> TrackingIssues { get; init; }

        public int TotalBuilds { get; init; }

        internal struct FailedBuild
        {
            public long Id { get; init; }

            public required string Number { get; init; }

            public IReadOnlyList<FailureDetail>? Details { get; init; }
        }

        internal class FailureDetail
        {
            public required string Job { get; init; }
            public required string Task { get; init; }
            public required string Details { get; init; }
        }
    }
}
