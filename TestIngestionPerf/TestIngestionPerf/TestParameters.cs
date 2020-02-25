using System;
using System.Collections.Generic;

namespace TestIngestionPerf
{
    public class TestParameters
    {
        public PackagePusher PackagePusher { get; set; }
        public string ApiKey { get; set; }
        public string IdPattern { get; set; }
        public string VersionPattern { get; set; }

        public IReadOnlyList<IEndpointChecker> EndpointCheckers { get; set; }

        public TimeSpan TestDuration { get; set; }
        public int PackageCount { get; set; }

        public Action<PackageResult> OnPackageResult { get; set; }
    }
}
