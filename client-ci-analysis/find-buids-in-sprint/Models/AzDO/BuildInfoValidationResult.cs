using System.Diagnostics;

namespace find_buids_in_sprint.Models.AzDO
{
    [DebuggerDisplay("{result}: {message}")]
    internal class BuildInfoValidationResult
    {
        public string result { get; set; }
        public string message { get; set; }
    }
}