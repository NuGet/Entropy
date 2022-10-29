using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace ReleaseNotesGenerator
{
    class Options
    {
        [Value(0, Required = true, HelpText = "Repository to get the issues from.")]
        public string Repo { get; set; }

        [Value(1, Required = true, HelpText = "Release version to generate the release notes for.")]
        public string Release { get; set; }

        [Option('g', "github-token", Required = false, HelpText = "GitHub Token for Auth. If not specified, it will acquired automatically.")]
        public string GitHubToken { get; set; }

        [Option("start-commit", Required = false, HelpText = "The starting sha for the current release. This commit *must* be on the release branch.")]
        public string StartSha { get; set; }

        [Option("end-commit", Required = false, HelpText = "The starting sha for the current release. This commit *must* be on the release branch. ")]
        public string EndSha { get; set; }


        [Usage(ApplicationAlias = "release-notes-generator")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example("Generate release notes for a particular release", new Options { Repo = "NuGet/Home", Release = "6.3", GitHubToken = "asdf" })
                };
            }
        }
    }
}
