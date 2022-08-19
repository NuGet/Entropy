using ci_testfailure_analyzer.Models.AzDO;
using System.Collections.Generic;

namespace ci_testfailure_analyzer.Models.ClientCiAnalysis
{
    internal class WeekBuilds
    {
        public List<BuildInfo> Official { get; } = new List<BuildInfo>();
        public List<BuildInfo> PullRequest { get; } = new List<BuildInfo>();

    }
}
