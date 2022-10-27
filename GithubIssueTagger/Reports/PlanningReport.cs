using Newtonsoft.Json;
using Octokit;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports
{
    internal class PlanningReport : IReport
    {
        private GitHubClient _client;

        public PlanningReport(GitHubClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task RunAsync()
        {
            await PlanningUtilities.RunPlanningAsync(_client);
        }
    }
}
