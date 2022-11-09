using NuGetReleaseTool.GenerateInsertionChangelogCommand;
using Octokit;

namespace NuGetReleaseTool.GenerateReleaseChangelogCommand
{
    public class ReleaseChangelogGenerator
    {
        private readonly GitHubClient GitHubClient;
        private readonly GenerateReleaseChangelogCommandOptions Options;

        public ReleaseChangelogGenerator(GenerateReleaseChangelogCommandOptions opts, GitHubClient gitHubClient)
        {
            Options = opts;
            GitHubClient = gitHubClient;
        }


        public async Task RunAsync()
        {
            var directory = Options.Output ?? Directory.GetCurrentDirectory();

            var githubCommits = await Helpers.GetCommitsForRelease(GitHubClient, Options.Release, Options.EndCommit);
            List<CommitWithDetails> commits = await Helpers.GetCommitDetails(
                GitHubClient,
                Constants.NuGet,
                Constants.NuGetClient,
                issueRepositories: new string[] { "NuGet/Home", "NuGet/Client.Engineering" },
                githubCommits);
            Helpers.SaveAsMarkdown(commits, directory);
        }

    }
}
