using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports
{
    internal class AllUnprocessed : IReport
    {
        private GitHubClient _client;
        private static IList<Issue> _unprocessedIssues;

        public AllUnprocessed(GitHubClient client)
        {
            _client = client;
        }

        public async Task Run()
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
    }
}
