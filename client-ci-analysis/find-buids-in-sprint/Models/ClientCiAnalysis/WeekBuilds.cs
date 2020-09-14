using find_buids_in_sprint.Models.AzDO;
using System.Collections.Generic;

namespace find_buids_in_sprint.Models.ClientCiAnalysis
{
    internal class WeekBuilds
    {
        public List<BuildInfo> Official { get; } = new List<BuildInfo>();
        public List<BuildInfo> PullRequest { get; } = new List<BuildInfo>();

    }
}
