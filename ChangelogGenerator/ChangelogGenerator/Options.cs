﻿using CommandLine;
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

        [Option('g', "github-token", Required = true, HelpText = "GitHub Token for Auth.")]
        public string GitHubToken { get; set; }

        [Option('z', "zenhub-token", Required = true, HelpText = "ZenHub Token for Auth.")]
        public string ZenHubToken { get; set; }

        [Option('l', "label", HelpText = "Show only those issues from the selected release that have this label.")]
        public string RequiredLabel { get; set; }

        [Option('v', "verbose", HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }

        [Option('o', "include-open", HelpText = "Include open issues.")]
        public bool IncludeOpen { get; set; }

        [Usage(ApplicationAlias = "changelog-generator")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example("Generate changelog for a particular release", new Options { Repo = "NuGet/Home", Release = "5.6", GitHubToken = "asdf", ZenHubToken = "hjkl" })
                };
            }
        }
    }
}
