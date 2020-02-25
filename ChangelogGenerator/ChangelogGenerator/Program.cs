using CommandLine;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangelogGenerator
{
    class Options
    {
        [Option('r', "repo", Required = true, HelpText = "Repo name to get the issues from")]
        public string Repo { get; set; }

        [Option('o', "Organization", Required = true, HelpText = "Name of the organization the repo belongs to")]
        public string Organization { get; set; }

        [Option('t', "GitHubToken", Required = true, HelpText = "GitHub Token for Auth")]
        public string GitHubToken { get; set; }

        [Option('m', "Milestone", Required = true, HelpText = "Milestone to get issues from")]
        public string Milestone { get; set; }

        [Option('l', "RequiredLabel", Required = true, HelpText = "Show only those issues from the selected milestone that have this label")]
        public string RequiredLabel { get; set; }

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

    class Program
    {
        static GitHubClient client = new GitHubClient(new ProductHeaderValue("my-cool-app"));
        static string title = string.Empty;
        static string changeloglink = string.Empty;
        static string issueslink = string.Empty;
        static Options options = new Options();

        static void Main(string[] args)
        {
            if (Parser.Default.ParseArguments(args, options))
            {
                // consume Options instance properties
                if (options.Verbose)
                {
                    Console.WriteLine(options.Organization);
                    Console.WriteLine(options.Repo);
                    Console.WriteLine(options.Milestone);
                }
                else
                {
                    Console.WriteLine("working ...");
                }
            }
            else
            {
                Console.WriteLine(options.GetUsage());
            }

            var tokenAuth = new Credentials(options.GitHubToken);
            client.Credentials = tokenAuth;
            GetChangelog();
            Console.ReadLine();
        }

        private static async void GetChangelog()
        {
            try
            {
                var issueFilter = new RepositoryIssueRequest()
                {
                    Filter = IssueFilter.All,
                    //Creator = "*",
                    //Milestone = options.Milestone,
                    State = ItemStateFilter.Closed,
                    //Since = new DateTimeOffset(new DateTime(2013, 1, 1)),
                  //  State = options.IncludeOpen.ToLower() == "y" ? ItemStateFilter.All : ItemStateFilter.Closed,
                };
                if (!string.IsNullOrEmpty(options.RequiredLabel))
                {
                  //  issueFilter.Labels.Add(options.RequiredLabel);
                }

                var issues = await client.Issue.GetAllForRepository(options.Organization, options.Repo, issueFilter);
                
                Dictionary<IssueType, List<Issue>> IssuesByIssueType = new Dictionary<IssueType, List<Issue>>();
                foreach (var issue in issues)
                {
                    if (issue.Milestone != null && issue.Milestone.Title == options.Milestone)
                    {
                        bool issueFixed = true;
                        bool hidden = false;
                        IssueType issueType = IssueType.None;
                        bool epicLabel = false;
                        bool regressionDuringThisVersion = false;
                        bool engineeringImprovement = false;
                        string requiredLabel = options.RequiredLabel?.ToLower();
                        bool foundRequiredLabel = string.IsNullOrEmpty(requiredLabel);

                        foreach (var label in issue.Labels)
                        {
                            if (label.Name.Contains("ClosedAs:"))
                            {
                                issueFixed = false;
                            }

                            if (label.Name == "RegressionDuringThisVersion")
                            {
                                regressionDuringThisVersion = true;
                                hidden = true;
                            }

                            if (label.Name == "Area: Engineering Improvements")
                            {
                                engineeringImprovement = true;
                                hidden = true;
                            }

                            if (!foundRequiredLabel && label.Name.ToLower() == requiredLabel)
                            {
                                foundRequiredLabel = true;
                            }

                            switch (label.Name)
                            {
                                case "Epic":
                                    epicLabel = true;
                                    break;
                                case "Type:Feature":
                                    issueType = IssueType.Feature;
                                    break;
                                case "Type:DCR":
                                    issueType = IssueType.DCR;
                                    break;
                                case "Type:Bug":
                                    issueType = IssueType.Bug;
                                    break;
                                case "Type:Spec":
                                    issueType = IssueType.Spec;
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (!foundRequiredLabel)
                        {
                            hidden = true;
                        }

                        // if an issue is an epicLabel and has a real IssueType (feature/bug/dcr),
                        // then hide it... we want to show the primary epic issue only.
                        if (epicLabel)
                        {
                            if (issueType == IssueType.None)
                            {
                                issueType = IssueType.Feature;
                            }
                            else
                            {
                                hidden = true;
                            }
                        }
                        else if (issueType == IssueType.None)
                        {
                            if (!(issueFixed && !regressionDuringThisVersion && !engineeringImprovement))
                            {
                                hidden = true;
                            }
                        }

                        if (!hidden && issueFixed)
                        {
                            if (!IssuesByIssueType.ContainsKey(issueType))
                            {
                                IssuesByIssueType.Add(issueType, new List<Issue>());
                            }

                            IssuesByIssueType[issueType].Add(issue);
                        }
                    }
                }

                GenerateMarkdown(IssuesByIssueType);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void GenerateMarkdown(Dictionary<IssueType, List<Issue>> labelSet)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("#" + options.Milestone + (!string.IsNullOrEmpty(options.RequiredLabel) ? "-" + options.RequiredLabel.ToLower() : "") + " Release Notes");
            builder.AppendLine();
            builder.AppendLine();
            builder.Append("[Full Changelog]" + "(" + changeloglink + ")");
            builder.AppendLine();
            builder.AppendLine();
            builder.Append("[Issues List]"
                + "(" + "https://github.com/"
                + options.Organization + "/"
                + options.Repo + "/"
                + "issues?q=is%3Aissue+is%3Aclosed"+ (!string.IsNullOrEmpty(options.RequiredLabel) ? "+label:" + options.RequiredLabel : "") + "+milestone%3A%22"
                + options.Milestone
                + "\")");
            builder.AppendLine();
            builder.AppendLine();
            foreach (var key in labelSet.Keys)
            {
                var issueTypeString = key.ToString();
                builder.Append("**" + issueTypeString + ":**");
                builder.AppendLine();
                builder.AppendLine();
                foreach (var issue in labelSet[key])
                {
                    builder.Append("* " + issue.Title + " - " + "[#" + issue.Number + "](" + issue.HtmlUrl + ")");
                    builder.AppendLine();
                    builder.AppendLine();
                }
            }

            var fileName = "Changelog-" + options.Milestone
                + (string.IsNullOrEmpty(options.RequiredLabel) ? "" : options.RequiredLabel) + ".md";

            File.WriteAllText(fileName, builder.ToString());
            Console.WriteLine($"{fileName} creation complete");
            Environment.Exit(0);
        }
    }
}
