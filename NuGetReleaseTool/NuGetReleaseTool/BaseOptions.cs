using CommandLine.Text;
using CommandLine;

namespace NuGetReleaseTool
{
    public class BaseOptions
    {
        [Option('g', "github-token", Required = false, HelpText = "GitHub Token for Auth. If not specified, it will acquired automatically.")]
        public string? GitHubToken { get; set; }
    }
}
