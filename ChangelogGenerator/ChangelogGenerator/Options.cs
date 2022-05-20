using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace ChangelogGenerator
{
    class Options
    {
        [Value(0, Required = true, HelpText = "Repository to get the issues from.")]
        public string Repo { get; set; }

        [Value(1, Required = true, HelpText = "Release version to generate a changelog for.")]
        public string Release { get; set; }

        [Option('g', "github-token", Required = false, HelpText = "GitHub Token for Auth. If not specified, it will acquired automatically.")]
        public string GitHubToken { get; set; }


        [Option('l', "label", HelpText = "Show only those issues from the selected release that have this label.")]
        public string RequiredLabel { get; set; }


        [Usage(ApplicationAlias = "changelog-generator")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example("Generate changelog for a particular release", new Options { Repo = "NuGet/Home", Release = "6.3", GitHubToken = "asdf" })
                };
            }
        }
    }
}
