using System.Diagnostics;

namespace ci_testfailure_analyzer.Models.AzDO
{
    [DebuggerDisplay("{name} ({id})")]
    internal class Definition
    {
        public uint id { get; set; }
        public string name { get; set; }
    }
}
