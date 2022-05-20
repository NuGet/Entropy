using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports
{
    internal class ClientEngineeringPriority1Issues : IReport
    {
        private GitHubClient _client;
        private static IEnumerable<Issue> _allClientEngineeringIssues;

        public ClientEngineeringPriority1Issues(GitHubClient client)
        {
            _client = client;
        }

        public async Task Run()
        {
            if (_allClientEngineeringIssues is null)
            {
                _allClientEngineeringIssues = await IssueUtilities.GetOpenPriority1Issues(_client, "nuget", "client.engineering");
            }

            var outputFileName = "clientEngineeringIssues.json";
            var json = JsonConvert.SerializeObject(_allClientEngineeringIssues, Formatting.Indented);
            File.WriteAllText(outputFileName, json);

            Console.WriteLine(nameof(ClientEngineeringPriority1Issues) + " wrote to " + outputFileName);
        }
    }
}
