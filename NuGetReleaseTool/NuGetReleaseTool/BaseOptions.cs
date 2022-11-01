using CommandLine;

namespace NuGetReleaseTool
{
    public class BaseOptions
    {
        [Value(0, Required = true, HelpText = "CommandToExecute")]
        public string Command { get; set; }
    }
}
