using System.Diagnostics;

namespace find_buids_in_sprint.Models.AzDO
{
    [DebuggerDisplay("{name} ({id})")]
    internal class Definition
    {
        public uint id { get; set; }
        public string name { get; set; }
    }
}
