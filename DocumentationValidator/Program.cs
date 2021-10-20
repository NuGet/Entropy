using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NuGet.Common;
using Octokit;

namespace DocumentationValidator
{
    class Program
    {
        static readonly string LogCodeTemplate = "https://docs.microsoft.com/en-us/nuget/reference/errors-and-warnings/{0}";

        static async Task<int> Main(string[] args)
        {
            IList<NuGetLogCode> allLogCodes = GetNuGetLogCodes();
            List<NuGetLogCode> undocumentedLogCodes = new();
            HttpClient httpClient = new();

            Console.WriteLine($"Checking {allLogCodes.Count} log codes for documentation.");

            for (int i = 0; i < allLogCodes.Count; i++)
            {
                if (i % 20 == 0 && i != 0) // The check will take some time, so display progress updates.
                {
                    Console.WriteLine($"Checked {i} of {allLogCodes.Count}");
                }

                if (!await IsLogCodeDocumentedAsync(httpClient, allLogCodes[i]))
                {
                    undocumentedLogCodes.Add(allLogCodes[i]);
                }
            }
            Console.WriteLine("Completed checking log codes for documentation.");

            Console.WriteLine($"Undocumented Log Codes Count {undocumentedLogCodes.Count}");

            if (undocumentedLogCodes.Count > 0)
            {
                var issuesForLogCodes = await GetIssuesForLogCodeAsync(undocumentedLogCodes);
                Console.Error.WriteLine("| Log Code | Potential Issues |");
                Console.Error.WriteLine("|----------|------------------|");

                foreach (var logCode in issuesForLogCodes)
                {
                    Console.Error.WriteLine($"| {logCode.Key} | {string.Join(" ,", logCode.Value)} |");
                }
            }
            return undocumentedLogCodes.Any() ? 1 : 0;
        }

        private static async Task<Dictionary<NuGetLogCode, List<string>>> GetIssuesForLogCodeAsync(List<NuGetLogCode> logCodes)
        {
            Dictionary<NuGetLogCode, List<string>> issues = new(logCodes.Count);
            foreach (var logCode in logCodes)
            {
                issues.Add(logCode, new List<string>());
            }

            var client = new GitHubClient(new ProductHeaderValue("nuget-documentation-validator"));

            IEnumerable<Issue> githubIssues = await GetAllIssues(client, "nuget", "docs.microsoft.com-nuget");

            foreach (Issue githubIssue in githubIssues)
            {
                foreach (var logCode in logCodes)
                {
                    if (IsIssueRelevantForLogCode(githubIssue, logCode))
                    {
                        issues[logCode].Add(githubIssue.HtmlUrl);
                    }
                }
            }

            return issues;
        }

        private static bool IsIssueRelevantForLogCode(Issue githubIssue, NuGetLogCode logCode)
        {
            return githubIssue.Title.Contains(logCode.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<IEnumerable<Issue>> GetAllIssues(GitHubClient client, string org, string repo)
        {
            return await client.Issue.GetAllForRepository(
                org,
                repo,
                new RepositoryIssueRequest
                {
                    Filter = IssueFilter.All
                }
                );
        }

        private static async Task<bool> IsLogCodeDocumentedAsync(HttpClient httpClient, NuGetLogCode logCode)
        {
            var result = await httpClient.GetAsync(string.Format(LogCodeTemplate, logCode));
            return result.IsSuccessStatusCode;
        }

        private static IList<NuGetLogCode> GetNuGetLogCodes()
        {
            var list = GetEnumList<NuGetLogCode>(); ;
            list.Remove(NuGetLogCode.Undefined);
            return list;

            IList<T> GetEnumList<T>()
            {
                T[] array = (T[])Enum.GetValues(typeof(T));
                List<T> list = new List<T>(array);
                return list;
            }
        }
    }
}
