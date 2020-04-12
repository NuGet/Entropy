using CommandLine;

using CommandLine.Text;

using System.Collections.Generic;

namespace UpdatePipelineLabel

{

    class Options

    {

        [Value(0, Required = true, HelpText = "Repository to get the issues from.")]

        public string Repo { get; set; }


        [Value(1, Required = true, HelpText = "GitHub Token for Auth.")]

        public string GitHubToken { get; set; }



        [Value(2, Required = true, HelpText = "ZenHub Token for Auth.")]

        public string ZenHubToken { get; set; }



        [Option('f', "from", HelpText = "Process Issue range from Number ")]

        public int IssueNumFrom { get; set; }



        [Option('t', "to", HelpText = "Process Issue range to Number")]

        public int IssueNumTo { get; set; }


        [Usage(ApplicationAlias = "update-pipeline-label")]

        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example("Update label for all issues (recommended)", new Options { Repo = "NuGet/Home", GitHubToken = "githubToken", ZenHubToken = "zenhubToken"}),
                    new Example("Update label for issues in the range (rarely used, only used when large number of issues need update)", new Options { Repo = "NuGet/Home", GitHubToken = "githubToken", ZenHubToken = "zenhubToken", IssueNumFrom = 100, IssueNumTo = 900})
                };
            }

        }

    }

}