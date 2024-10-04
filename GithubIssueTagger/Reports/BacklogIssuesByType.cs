using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports
{
    internal class BacklogIssuesByType : IReport
    {
        private GitHubClient _client;
        private QueryCache _queryCache;

        public BacklogIssuesByType(GitHubClient client, QueryCache queryCache)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _queryCache = queryCache ?? throw new ArgumentNullException(nameof(queryCache));
        }

        public async Task RunAsync()
        {
            var issues = await IssueUtilities.GetIssuesForAnyMatchingLabelsAsync(_client, "nuget", "home", "Priority:1", "Priority:2");
            var outputFileName = "home.issues";
            var json = JsonConvert.SerializeObject(issues, Formatting.Indented);
            File.WriteAllText(outputFileName, json);

            Dictionary<string, int> keyValuePairs = new Dictionary<string, int>()
            {
                { "Type:Bug", 0 },
                { "Type:DCR", 0 },
                { "Type:Feature", 0 },
                { "Type:Test", 0 },
                { "Type:Engineering", 0 },
                { "Type:Tracking", 0 },
                { "Type:DataAnalysis", 0 },
                { "Type:Docs", 0 },
                { "Type:DeveloperDocs", 0 },
                { "Type:Spec", 0 },
            };

            foreach (var issue in issues)
            {
                foreach (var label in issue.Labels)
                {
                    if (keyValuePairs.ContainsKey(label.Name))
                    {
                        keyValuePairs[label.Name] += 1;
                    }
                }
            }
            foreach (var kvp in keyValuePairs)
            {
                Console.WriteLine($"{kvp.Key} : {kvp.Value}");
            }
        }
    }
}
