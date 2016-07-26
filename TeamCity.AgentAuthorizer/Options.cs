using System;

namespace NuGet.TeamCity.AgentAuthorizer
{
    public class Options
    {
        public string Server { get; set; }
        public string AgentName { get; set; }
        public bool AgentEnabled { get; set; }
        public TimeSpan Timeout { get; set; }
        public string AgentPoolName { get; set; }
    }
}
