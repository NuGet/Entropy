using Octokit;
using System.Text.RegularExpressions;

namespace NuGetReleaseTool.GenerateInsertionChangelogCommand
{
    public class ChangeLogGenerator
    {
        public static async Task GenerateInsertionChangelogForNuGetClient(GitHubClient gitHubClient, string startSha, string branchName, string resultPath)
        {
            // Adjust the sha/repo
            var orgName = "nuget";
            var repoName = "nuget.client";
            string[] issueRepositories = new string[] { "NuGet/Home", "NuGet/Client.Engineering" };

            var githubBranch = await gitHubClient.Repository.Branch.Get(orgName, repoName, branchName);
            var githubCommits = (await gitHubClient.Repository.Commit.Compare(orgName, repoName, startSha, githubBranch.Commit.Sha)).Commits.Reverse();
            List<CommitWithDetails> commits = await Helpers.GetCommitDetails(gitHubClient, orgName, repoName, issueRepositories, githubCommits);
            Helpers.SaveAsHtml(commits, resultPath);
            Helpers.SaveAsMarkdown(commits, resultPath);
        }
    }
}
