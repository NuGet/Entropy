using System.Collections.Generic;
using System.Diagnostics;

namespace find_buids_in_sprint.Models.AzDO
{
    [DebuggerDisplay("({count})")]
    internal class BuildList
    {
        public uint count { get; set; }
        public List<BuildInfo> value { get; set; }
    }
}
