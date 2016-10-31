using CommandLine;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadMeGenerator
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

                var issues = await client.Issue.GetAllForRepository(options.Organization, options.Repo, shouldPrioritize);
                Dictionary<string, List<Issue>> labelSet = new Dictionary<string, List<Issue>>();
                foreach (var issue in issues)
                {
                    if (issue.Milestone?.Title == options.Milestone)
                    {
                        issuesList.Add(issue);

                        foreach (var label in issue.Labels)
                        {
                            if (label.Name.Contains("Type"))
                            {
                                if (labelSet.ContainsKey(label.Name) && !label.Name.Contains("ClosedAs:External"))
                                {
                                    labelSet[label.Name].Add(issue);
                                    continue;
                                }

                                labelSet.Add(label.Name, new List<Issue>() { issue });
                            }
                        }
                    }
                }

                GenerateMarkdown(labelSet);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void GenerateMarkdown(Dictionary<string, List<Issue>> labelSet)
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
                builder.Append("**" + key.Replace("Type:", "") + ":**");
                builder.AppendLine();
                builder.AppendLine();
                foreach (var issue in labelSet[key])
                {
                    builder.Append("* " + issue.Title + " - " + "[#" + issue.Number + "](" + issue.HtmlUrl + ")");
                    builder.AppendLine();
                    builder.AppendLine();
                }
            }

            File.WriteAllText("Changelog.md", builder.ToString());
            Console.WriteLine("Bazzinga.............");
            Environment.Exit(0);
        }
    }
}
