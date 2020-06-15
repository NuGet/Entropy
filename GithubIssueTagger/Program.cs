using Octokit;
using System;
using System.Threading.Tasks;

namespace GithubIssueTagger
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Expected 1 argument (github PAT). Found " + args.Length);
                return;
            }

            var client = new GitHubClient(new ProductHeaderValue("nuget-github-issue-tagger"))
            {
                Credentials = new Credentials(args[0])
            };

            await PlanningUtilities.RunPlanningAsync(client);
        }
    }
}
