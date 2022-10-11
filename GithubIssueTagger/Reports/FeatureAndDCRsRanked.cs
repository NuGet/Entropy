using Newtonsoft.Json;
using Octokit;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports
{
    internal class FeatureAndDCRsRanked : IReport
    {
        private GitHubClient _client;

        public FeatureAndDCRsRanked(GitHubClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task RunAsync()
        {
            await IssueUtilities.GetIssuesRankedAsync(_client, "Type:DCR", "Type:Feature");
        }
    }
}
