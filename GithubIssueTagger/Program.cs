using Octokit;
using System.Threading.Tasks;

namespace GithubIssueTagger
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var client = new GitHubClient(new ProductHeaderValue("nuget-github-issue-tagger"));
            client.Credentials = new Credentials(args[0]);
            await PullRequestUtilities.ProcessPullRequestStatsAsync(client);
        }
    }
}
