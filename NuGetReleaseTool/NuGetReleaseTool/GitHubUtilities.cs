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

        private static string GetReleaseBranchFromVersion(Version parsedVersion)
        {
            return $"release-{parsedVersion.Major}.{parsedVersion.Minor}.x";
        }

        public static string GetLatestTagForMajorMinor(Version currentVersion, IReadOnlyList<RepositoryTag> allTags)
        {
            var latestTag = allTags.Where(e => e.Name.StartsWith($"{currentVersion.Major}.{currentVersion.Minor}")).Select(e => Version.Parse(e.Name)).Max();
            if (latestTag != null)
            {
                return latestTag.ToString();
            }
            throw new InvalidOperationException($"The {currentVersion} does not have any tags");
        }

        public static Version EstimatePreviousMajorMinorVersion(Version currentVersion, IReadOnlyList<RepositoryTag> allTags)
        {
            if (currentVersion.Minor > 0)
            {
                return new Version(currentVersion.Major, currentVersion.Minor - 1);
            }
            else
            {
                var tagsWithPreviousMajor = allTags.Where(e => e.Name.StartsWith(currentVersion.Major - 1 + "."));
                Version? maxTagVersion = tagsWithPreviousMajor.Select(e => Version.Parse(e.Name)).Max();
                if (maxTagVersion == null)
                {
                    throw new Exception($"Cannot infer previous major/minor version from the tags. Current version is {currentVersion}.");
                }
                return new Version(maxTagVersion.Major, maxTagVersion.Minor);
            }
        }

        public static async Task<List<GitHubCommit>> GetCommitsForRelease(GitHubClient gitHubClient, string releaseVersion, string endCommit)
        {
            var version = new Version(releaseVersion);
            var currentReleaseBranchName = GetReleaseBranchFromVersion(version);
            var previousReleaseBranchName = GetReleaseBranchFromVersion(EstimatePreviousMajorMinorVersion(version, await gitHubClient.Repository.GetAllTags(Constants.NuGet, Constants.NuGetClient)));
            return await GetUniqueCommitsListBetween2Branches(gitHubClient, Constants.NuGet, Constants.NuGetClient, previousReleaseBranchName, currentReleaseBranchName, endCommit);
        }

        public static async Task<List<GitHubCommit>> GetUniqueCommitsListBetween2Branches(GitHubClient gitHubClient, string orgName, string repoName, string previousBranchName, string currentBranchName, string? latestShaOnCurrentBranch = null)
        {
            var previousBranch = await gitHubClient.Repository.Branch.Get(orgName, repoName, previousBranchName);
            var currentBranch = await gitHubClient.Repository.Branch.Get(orgName, repoName, currentBranchName);

            // Reverse so that the oldest commit is at the top.
            string latestShaToUse = latestShaOnCurrentBranch ?? currentBranch.Commit.Sha;
            var allCommitDifference = (await gitHubClient.Repository.Commit.Compare(orgName, repoName, previousBranch.Commit.Sha, latestShaToUse)).Commits.Reverse();

            var commitsOnReleaseBranchSince = await gitHubClient.Repository.Commit.GetAll(orgName, repoName, new CommitRequest
            {
                Since = allCommitDifference.Min(e => e.Commit.Committer.Date), // Find the oldest commit in the delta
                Sha = previousBranch.Commit.Sha
            });

            List<GitHubCommit> gitHubCommits = new();

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
            static string RemovePRLabel(string message)
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
}