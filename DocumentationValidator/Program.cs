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
            HttpClient httpClient = new();
            var cred = args.Length > 0 ? args[0] : "123";
            var client = new GitHubClient(new ProductHeaderValue("nuget-documentation-validator"))
            {
                Credentials = new Credentials(cred)
            };

            List<NuGetLogCode> undocumentedLogCodes = await GetUndocumentedLogCodes(allLogCodes, httpClient);
            Console.WriteLine($"Undocumented Log Codes Count {undocumentedLogCodes.Count}");

            Dictionary<NuGetLogCode, List<string>> logCodeToIssue =
                await GetIssuesForLogCodeAsync(client, undocumentedLogCodes);
            PrintIssues(logCodeToIssue);

            bool shouldCreateIssues = false;
            _ = args.Length >= 2 ? bool.TryParse(args[1], out shouldCreateIssues) : false;

            if (shouldCreateIssues)
            {
                Console.WriteLine($"Creating issues for log codes without one.");
                foreach (var logCodeIssueKVP in logCodeToIssue)
                {
                    if (logCodeIssueKVP.Value.Count == 0)
                    {
                        await CreateIssueForLogCodeInDocsRepo(client, logCodeIssueKVP);
                    }
                }
            }

            PrintIssues(logCodeToIssue);

            return shouldCreateIssues ?
                0 :
                undocumentedLogCodes.Any() ? 1 : 0;
        }

        private static async Task CreateIssueForLogCodeInDocsRepo(GitHubClient client, KeyValuePair<NuGetLogCode, List<string>> logCodeIssueKVP)
        {
            var newIssue = new NewIssue(title: $"Document NuGet Code: {logCodeIssueKVP.Key}");
            newIssue.Body = "The NuGet Code exists in the product, but there is no documentation available";
            newIssue.Labels.Add("Team:Client");
            newIssue.Labels.Add("P1");
            newIssue.Labels.Add("doc-bug");
            var issue = await client.Issue.Create("nuget", "docs.microsoft.com-nuget", newIssue);
            Console.WriteLine($"Created issue {issue.HtmlUrl} for log code {logCodeIssueKVP.Key}");
            logCodeIssueKVP.Value.Add(issue.HtmlUrl);
        }

        private static void PrintIssues(Dictionary<NuGetLogCode, List<string>> issuesForLogCodes)
        {
            Console.Error.WriteLine("| Log Code | Potential Issues |");
            Console.Error.WriteLine("|----------|------------------|");

            foreach (var logCode in issuesForLogCodes)
            {
                Console.Error.WriteLine($"| {logCode.Key} | {string.Join(" ,", logCode.Value)} |");
            }
        }

        private static async Task<List<NuGetLogCode>> GetUndocumentedLogCodes(IList<NuGetLogCode> allLogCodes, HttpClient httpClient)
        {
            Console.WriteLine($"Checking {allLogCodes.Count} log codes for documentation.");
            List<NuGetLogCode> undocumentedLogCodes = new();
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
            return undocumentedLogCodes;
        }

        private static async Task<Dictionary<NuGetLogCode, List<string>>> GetIssuesForLogCodeAsync(GitHubClient client, List<NuGetLogCode> logCodes)
        {
            Dictionary<NuGetLogCode, List<string>> issues = new(logCodes.Count);

            foreach (var logCode in logCodes)
            {
                issues.Add(logCode, new List<string>());
            }
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
