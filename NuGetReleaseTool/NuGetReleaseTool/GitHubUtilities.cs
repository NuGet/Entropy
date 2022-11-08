using Octokit;
using System.Text.RegularExpressions;

namespace NuGetReleaseTool
{
    internal static class GitHubUtilities
    {
        public static string GetReleaseBranchFromVersion(string version)
        {
            var parsedVersion = new Version(version);
            return $"release-{parsedVersion.Major}.{parsedVersion.Minor}.x";
        }

        public static async Task<List<GitHubCommit>> GetUniqueCommitsListBetween2Branches(GitHubClient gitHubClient, string orgName, string repoName, string previousBranchName, string currentBranchName)
        {
            var previousBranch = await gitHubClient.Repository.Branch.Get(orgName, repoName, previousBranchName);
            var currentBranch = await gitHubClient.Repository.Branch.Get(orgName, repoName, currentBranchName);
            List<GitHubCommit> gitHubCommits = await GetUniqueCommitListForCommitId(gitHubClient, orgName, repoName, previousBranch, currentBranch);

            return gitHubCommits;
        }

        private static async Task<List<GitHubCommit>> GetUniqueCommitListForCommitId(GitHubClient gitHubClient, string orgName, string repoName, Branch previousBranch, Branch currentBranch)
        {
            var repoDiff = await gitHubClient.Repository.Commit.Compare(orgName, repoName, previousBranch.Commit.Sha, currentBranch.Commit.Sha);
            var allCommitDifference = repoDiff.Commits.Reverse(); // Reverse so that the oldest commit is at the top.

            var commitRequest = new CommitRequest
            {
                Since = allCommitDifference.Min(e => e.Commit.Committer.Date), // Find the oldest commit in the delta
                Sha = previousBranch.Commit.Sha
            };
            var commitsOnReleaseBranchSince = await gitHubClient.Repository.Commit.GetAll(orgName, repoName, commitRequest);

            var gitHubCommits = new List<GitHubCommit>();

            foreach (var commit in allCommitDifference)
            {
                var commitMessage = commit.Commit.Message;
                var matchingCommitMessage = commitsOnReleaseBranchSince.FirstOrDefault(e => RemovePRLabel(e.Commit.Message).Contains(RemovePRLabel(commitMessage)));
                if (matchingCommitMessage == null)
                {
                    gitHubCommits.Add(commit);
                }
            }
            return gitHubCommits;
        }

        public static string RemovePRLabel(string message)
        {
            foreach (Match match in new Regex(@"\(#\d+\)", RegexOptions.RightToLeft).Matches(message))
            {
                // match={(#4634)}
                return message.Remove(match.Index, match.Length);
            }
            return message;
        }
    }
}