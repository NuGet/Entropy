using Newtonsoft.Json;
using Octokit;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports
{
    internal class HomePriority1Issues : IReport
    {
        private GitHubClient _client;
        private QueryCache _queryCache;

        public HomePriority1Issues(GitHubClient client, QueryCache queryCache)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _queryCache = queryCache ?? throw new ArgumentNullException(nameof(queryCache));
        }

        public async Task RunAsync()
        {
            if (_queryCache.AllHomeIssues is null)
            {
                _queryCache.AllHomeIssues = await IssueUtilities.GetOpenPriority1IssuesAsync(_client, "nuget", "home");
            }

            var outputFileName = "homeIssues.json";
            var json = JsonConvert.SerializeObject(_queryCache.AllHomeIssues, Formatting.Indented);
            File.WriteAllText(outputFileName, json);

            Console.WriteLine(nameof(HomePriority1Issues) + " wrote to " + outputFileName);
        }
    }
}
