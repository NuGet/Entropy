using System.Collections.Generic;

namespace nuget_sdk_usage
{
    internal class UsageInformation
    {
        public HashSet<string> TargetFrameworks { get; set; } = new HashSet<string>();

        public HashSet<string> Versions { get; set; } = new HashSet<string>();

        public Dictionary<string, HashSet<string>> MemberReferences { get; set; } = new Dictionary<string, HashSet<string>>();
    }
}
