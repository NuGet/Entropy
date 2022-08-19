using System.Collections.Generic;
using System.Diagnostics;

namespace ci_testfailure_analyzer.Models.AzDO
{
    [DebuggerDisplay("({count})")]
    internal class BuildList
    {
        public uint count { get; set; }
        public List<BuildInfo> value { get; set; }
    }
}
