using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using NuGet.Common;
using Octokit;


namespace DocumentationValidator
{
    internal class NuGetLogCodeAnalyzer
    {
        readonly HttpClient _httpClient;
        readonly GitHubClient _gitHubClient;
        readonly TextWriter _logger;

        internal NuGetLogCodeAnalyzer(string pat, TextWriter logger)
        {
            _httpClient = new();
            _gitHubClient = new GitHubClient(new ProductHeaderValue("nuget-documentation-validator"));

            if (!string.IsNullOrEmpty(pat))
            {
                _gitHubClient.Credentials = new Credentials(pat);
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal async Task<Dictionary<NuGetLogCode, List<string>>> GetUndocumentedLogCodesAsync()
        {
            IList<NuGetLogCode> allLogCodes = GetNuGetLogCodes();

            List<NuGetLogCode> undocumentedLogCodes = await GetUndocumentedLogCodes(allLogCodes, _httpClient);
            _logger.WriteLine($"Undocumented Log Codes Count {undocumentedLogCodes.Count}");

            return await GetIssuesForLogCodeAsync(_gitHubClient, undocumentedLogCodes);
        }

        internal async Task CreateIssuesAsync(Dictionary<NuGetLogCode, List<string>> logCodeToIssue)
        {
            _logger.WriteLine("Creating issues for log codes without one.");
            foreach (var logCodeIssueKVP in logCodeToIssue)
            {
                if (logCodeIssueKVP.Value.Count == 0)
                {
                    await CreateIssueForLogCodeInDocsRepoAsync(_gitHubClient, logCodeIssueKVP, _logger);
                }
            }

            static async Task CreateIssueForLogCodeInDocsRepoAsync(GitHubClient client, KeyValuePair<NuGetLogCode, List<string>> logCodeIssueKVP, TextWriter logger)
            {
                var newIssue = new NewIssue(title: $"Document NuGet Code: {logCodeIssueKVP.Key}")
                {
                    Body = "The NuGet Code exists in the product, but there is no documentation available"
                };
                newIssue.Labels.Add("Team:Client");
                newIssue.Labels.Add("P1");
                newIssue.Labels.Add("doc-bug");
                var issue = await client.Issue.Create("nuget", "docs.microsoft.com-nuget", newIssue);
                logger.WriteLine($"Created issue {issue.HtmlUrl} for log code {logCodeIssueKVP.Key}");
                logCodeIssueKVP.Value.Add(issue.HtmlUrl);
            }
        }

        private async Task<List<NuGetLogCode>> GetUndocumentedLogCodes(IList<NuGetLogCode> allLogCodes, HttpClient httpClient)
        {
            _logger.WriteLine($"Checking {allLogCodes.Count} log codes for documentation.");
            List<NuGetLogCode> undocumentedLogCodes = new();
            for (int i = 0; i < allLogCodes.Count; i++)
            {
                if (i % 20 == 0 && i != 0) // The check will take some time, so display progress updates.
                {
                    _logger.WriteLine($"Checked {i} of {allLogCodes.Count}");
                }

                if (!await IsLogCodeDocumentedAsync(httpClient, allLogCodes[i]))
                {
                    undocumentedLogCodes.Add(allLogCodes[i]);
                }
            }
            _logger.WriteLine("Completed checking log codes for documentation.");
            return undocumentedLogCodes;

            static async Task<bool> IsLogCodeDocumentedAsync(HttpClient httpClient, NuGetLogCode logCode)
            {
                string LogCodeTemplate = "https://docs.microsoft.com/en-us/nuget/reference/errors-and-warnings/{0}";

                var result = await httpClient.GetAsync(string.Format(LogCodeTemplate, logCode));
                return result.IsSuccessStatusCode;
            }
        }

        private static async Task<Dictionary<NuGetLogCode, List<string>>> GetIssuesForLogCodeAsync(GitHubClient client, List<NuGetLogCode> logCodes)
        {
            Dictionary<NuGetLogCode, List<string>> issues = new(logCodes.Count);
            foreach (var logCode in logCodes)
            {
                issues.Add(logCode, new List<string>());
            }

            IEnumerable<Issue> githubIssues = await GetAllIssuesAsync(client, "nuget", "docs.microsoft.com-nuget");

            foreach (Issue githubIssue in githubIssues)
            {
                foreach (var logCode in logCodes)
                {
                    if (githubIssue.Title.Contains(logCode.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        issues[logCode].Add(githubIssue.HtmlUrl);
                    }
                }
            }

            return issues;

            static async Task<IEnumerable<Issue>> GetAllIssuesAsync(GitHubClient client, string org, string repo)
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
        }

        private static IList<NuGetLogCode> GetNuGetLogCodes()
        {
            var list = GetEnumList<NuGetLogCode>(); ;
            list.Remove(NuGetLogCode.Undefined);
            return list;

            static IList<T> GetEnumList<T>()
            {
                T[] array = (T[])Enum.GetValues(typeof(T));
                List<T> list = new List<T>(array);
                return list;
            }
        }
    }
}
