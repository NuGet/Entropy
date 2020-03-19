using Octokit;
using System.Threading.Tasks;

namespace GithubIssueTagger
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            if (args.count != 1)
            {
                Console.Err.WriteLine("Expected 1 argument (github PAT). Found " + args.Count);
                return;
            }
            var client = new GitHubClient(new ProductHeaderValue("nuget-github-issue-tagger"));
            client.Credentials = new Credentials(args[0]);
            await PullRequestUtilities.ProcessPullRequestStatsAsync(client);
        }
    }
}
