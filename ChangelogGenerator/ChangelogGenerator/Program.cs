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

        [Option('v', null, HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }

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
        static List<Issue> issuesList = new List<Issue>();
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
                var shouldPrioritize = new RepositoryIssueRequest
                {
                    Filter = IssueFilter.All,
                    State = ItemStateFilter.Closed,
                };

                List<Issue> problemIssues = new List<Issue>();

                var issues = await client.Issue.GetAllForRepository(options.Organization, options.Repo, shouldPrioritize);
                Dictionary<IssueType, List<Issue>> labelSet = new Dictionary<IssueType, List<Issue>>();
                foreach (var issue in issues)
                {
                    if (issue.Milestone != null && issue.Milestone.Title == options.Milestone)
                    {
                        issuesList.Add(issue);

                        bool issueFixed = true;
                        bool hidden = false;
                        IssueType issueType = IssueType.None;
                        bool epicLabel = false;
                        bool regressionDuringThisVersion = false;

                        foreach (var label in issue.Labels)
                        {
                            if (label.Name.Contains("ClosedAs:"))
                            {
                                issueFixed = false;
                            }

                            if (label.Name=="RegressionDuringThisVersion")
                            {
                                regressionDuringThisVersion = true;
                                hidden = true;
                            }

                            if (label.Name == "Area: Engineering Improvements")
                            {
                                hidden = true;
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
                                default:
                                    break;
                            }
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
                        else if (issueType == IssueType.None )
                        {
                            if (issueFixed && !regressionDuringThisVersion)
                            {
                                // PROBLEM : if this is fixed...was it a feature/bug or dcr???
                                problemIssues.Add(issue);
                            }
                            else
                            {
                                hidden = true;
                            }
                        }


                        if (!hidden && issueFixed)
                        {
                            List<Issue> issueCollection = null;
                            if (!labelSet.ContainsKey(issueType))
                            {
                                issueCollection = new List<Issue>();
                                labelSet.Add(issueType, issueCollection);
                            }
                            else
                            {
                                issueCollection = labelSet[issueType];
                            }

                            issueCollection.Add(issue);
                        }
                    }
                }

                GenerateMarkdown(labelSet, problemIssues);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void GenerateMarkdown(Dictionary<IssueType, List<Issue>> labelSet, List<Issue> problemIssues)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("#" + options.Milestone + " Release Notes");
            builder.AppendLine();
            builder.AppendLine();
            builder.Append("[Full Changelog]" + "(" + changeloglink + ")");
            builder.AppendLine();
            builder.AppendLine();
            builder.Append("[Issues List]"
                + "(" + "https://github.com/"
                + options.Organization + "/"
                + options.Repo + "/"
                + "issues?q=is%3Aissue+is%3Aclosed+milestone%3A%22"
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

            if (problemIssues.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("*********** Problem Data - should be marked as Feature/Bug/DCR or closedAs something");

                foreach (var issue in problemIssues)
                {
                    string labelString = null;
                    foreach (var label in issue.Labels)
                    {
                        labelString += label + " ";
                    }

                    builder.AppendLine(issue.Number + " " + issue.Title + " labels: " + labelString);
                }
            }

            File.WriteAllText("Changelog.md", builder.ToString());
            Console.WriteLine("Bazzinga.............");
            Environment.Exit(0);
        }
    }
}
