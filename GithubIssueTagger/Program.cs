using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GithubIssueTagger
{
    public class Program
    {
        private static IList<Issue> _unprocessedIssues;
        private static IReadOnlyList<Label> _allLabels;
        private static GitHubClient _client;

        static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Expected 1 argument (github PAT). Found " + args.Length);
                return;
            }

            _client = new GitHubClient(new ProductHeaderValue("nuget-github-issue-tagger"))
            {
                Credentials = new Credentials(args[0])
            };

            await PromptForQuery();
        }

        private static async Task PromptForQuery()
        {
            Console.WriteLine("**********************************************************************");
            Console.WriteLine("******************* NuGet GitHub Issue Tagger ************************");
            Console.WriteLine("**********************************************************************");
            Console.WriteLine();

            do
            {
                Console.WriteLine("Enter a # to query:");
                Console.WriteLine("1: " + nameof(AllUnprocessed));
                Console.WriteLine("2: " + nameof(AllLabels));
            }
            while (null != await RunQueryOrReturnUnknownInput(Console.ReadLine()));
        }

        private static async Task<string> RunQueryOrReturnUnknownInput(string v)
        {
            if (v is null)
                return string.Empty;
            Console.Write("*** Executing... ");
            string executedMethod = string.Empty;
            switch (v.Trim())
            {
                case "1":
                    executedMethod = nameof(AllUnprocessed);
                    Console.WriteLine(executedMethod + "***");
                    await AllUnprocessed();
                    break;
                case "2":
                    executedMethod = nameof(AllLabels);
                    Console.WriteLine(executedMethod + "***");
                    await AllLabels();
                    break;
                case "quit":
                    return null;
                default:
                    break;
            }
            Console.WriteLine("*** Done Executing " + executedMethod + " ***");
            return string.Empty;
        }

        private static async Task AllUnprocessed()
        {
            if (_unprocessedIssues is null)
            {
                _unprocessedIssues = await IssueUtilities.GetUnprocessedIssues(_client, "nuget", "home");
            }
            foreach (var issue in _unprocessedIssues)
            {
                Console.WriteLine(issue.HtmlUrl);
            }
        }

        private static async Task AllLabels()
        {
            if (_allLabels is null)
            {
                _allLabels  = await LabelUtilities.GetLabelsForRepository(_client, "nuget", "home");
            }
            Console.WriteLine("(ID Name)");
            foreach (var label in _allLabels)
            {
                Console.WriteLine(label.Id + " " + label.Name);
            }
        }

        private static async Task AreaLabels()
        {
            if (_allLabels is null)
            {
                _allLabels = await LabelUtilities.GetLabelsForRepository(_client, "nuget", "home");
            }


        }
    }
}
