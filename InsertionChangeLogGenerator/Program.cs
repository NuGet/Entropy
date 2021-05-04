using Octokit;
using System;
using System.Threading.Tasks;

namespace InsertionChangeLogGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Expected 1 argument (github PAT). Found " + args.Length);
                return;
            }

            var githubClient = new GitHubClient(new ProductHeaderValue("nuget-github-insertion-changelog-tagger"))
            {
                Credentials = new Credentials(args[0])
            };

            string startSha = "58795a41a3c8c9d63801fb5072a2e68e48d7507e";
            string branchName = "dev";

            await ChangeLogGenerator.GenerateInsertionChangelogForNuGetClient(githubClient, startSha, branchName);
        }
    }
}
