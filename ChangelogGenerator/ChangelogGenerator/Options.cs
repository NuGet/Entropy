using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangelogGenerator
{
    internal class Options
    {
        [Option('r', "repo", Required = true, HelpText = "Repo name to get the issues from")]
        public string Repo { get; set; }

        [Option('o', "Organization", Required = true, HelpText = "Name of the organization the repo belongs to")]
        public string Organization { get; set; }

        [Option('t', "GitHubToken", Required = true, HelpText = "GitHub Token for Auth")]
        public string GitHubToken { get; set; }

        [Option('m', "Milestone", HelpText = "Milestone to get issues from")]
        public string Milestone { get; set; }

        [Option('l', "RequiredLabel", HelpText = "Show only those issues from the selected milestone that have this label")]
        public string RequiredLabel { get; set; }

        [Option('w', "WeekRange", HelpText = "Filter by created date. 0 means this week. -2,0 means 3 weeks including the current week.")]
        public string WeekRange { get; set; }

        [Option('v', null, HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }

        [Option('i', "IncludeOpen", HelpText = "Include open issues.")]
        public string IncludeOpen { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var usage = new StringBuilder();
            usage.AppendLine("Quickstart Application 1.0");
            usage.AppendLine("Read user manual for usage instructions...");
            return usage.ToString();
        }
    }

}
