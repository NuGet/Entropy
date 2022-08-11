using System.Diagnostics;

namespace ci_testfailure_analyzer.Models.AzDO
{
    [DebuggerDisplay("{result}: {message}")]
    internal class BuildInfoValidationResult
    {
        public string result { get; set; }
        public string message { get; set; }
    }
}
