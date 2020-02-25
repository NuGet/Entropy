using System;
using System.Collections.Generic;

namespace TestIngestionPerf
{
    public class PackageResult
    {
        public PackageResult(TestPackage package, DateTimeOffset started, TimeSpan pushDuration, IReadOnlyList<EndpointResult> endpointResults)
        {
            Package = package;
            Started = started;
            PushDuration = pushDuration;
            EndpointResults = endpointResults;
        }

        public TestPackage Package { get; }
        public DateTimeOffset Started { get; }
        public TimeSpan PushDuration { get; }
        public IReadOnlyList<EndpointResult> EndpointResults { get; }
    }
}
