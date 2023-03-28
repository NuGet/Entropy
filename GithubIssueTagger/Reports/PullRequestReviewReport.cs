using Octokit;
using System;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports
{
    internal class PullRequestReviewReport : IReport
    {
        private readonly GitHubClient _client;

        public PullRequestReviewReport(GitHubClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task RunAsync()
        {
            await PullRequestUtilities.ProcessPullRequestStatsAsync(_client, 
                fetchDataFromRemote: true, 
                preprocess: true, 
                analyzeReviewDistribution: true, 
                analyzeTimeToReview: true,
                oldestPR: 5000,
                newestPR: 5111);
        }
    }
}
