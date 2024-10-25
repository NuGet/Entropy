using NuGet.Protocol.Plugins;
using NuGetReleaseTool.GenerateInsertionChangelogCommand;
using Octokit;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NuGetReleaseTool
{
    internal static class Helpers
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

        public static async Task<List<GitHubCommit>> GetCommitsForRelease(GitHubClient gitHubClient, string releaseVersion, string? endCommit)
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
                // If a commit in the previous version's branch has one with an equivalent name in the current release branch, we skip that commit as it was part of the previous release.
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

        public static async Task<List<CommitWithDetails>> GetCommitDetails(GitHubClient gitHubClient, string orgName, string repoName, string[] issueRepositories, IEnumerable<GitHubCommit> githubCommits)
        {
            var processedCommits = new List<CommitWithDetails>();
            foreach (var ghCommit in githubCommits)
            {
                var author = ghCommit.Author?.Login ?? ghCommit.Committer?.Login ?? "";
                var commit = new CommitWithDetails(ghCommit.Sha, author, $"https://github.com/{orgName}/{repoName}/commit/{ghCommit.Sha}", ghCommit.Commit.Message);
                var id = GetPRId(ghCommit.Commit.Message);
                string pullRequestbody = string.Empty;
                if (id != -1)
                {
                    commit.PR = new Tuple<int, string>(id, @"https://github.com/nuget/nuget.client/pull/" + id);
                    pullRequestbody = (await gitHubClient.Repository.PullRequest.Get(orgName, repoName, id)).Body;

                }

                if (pullRequestbody == null)
                {
                    Console.WriteLine($"PR contains contains no body message: {commit.PR?.Item2}");
                }
                else
                {
                    UpdateCommitIssuesFromText(commit, pullRequestbody, issueRepositories);
                }

                processedCommits.Add(commit);
            }

            return processedCommits;

            static int GetPRId(string message)
            {
                //use RegexOptions.RightToLeft to match from the right side, to ignore the other numbers in the title
                //E.g. 	Fix spelling of Wiederherstellen (NuGet/Home#11774) (#4591)
                //Or, Revert "Disable timing out EndToEnd tests (#4592)" (#4597) Fixes https://github.com/NuGet/Client.Engineering/issues/1572 This reverts commit acee7c1c1773e3d96ca806b10ba068dd09b0baf5.
                foreach (Match match in new Regex(@"\(#\d+\)", RegexOptions.RightToLeft).Matches(message))
                {
                    // match={(#4634)}, pullRequestsIdText=4634
                    var pullRequestIdText = match.Value.Substring(2, match.Length - 3);
                    int.TryParse(pullRequestIdText, out int prId);
                    return prId;
                }

                return -1;
            }

            static void UpdateCommitIssuesFromText(
            CommitWithDetails commit,
            string body,
            IList<string> issueRepositories)
            {
                var issueUrls = issueRepositories.SelectMany(e => GetIssueLinks(body, e)).ToList();
                issueUrls.Remove("https://github.com/nuget/home/issues/1000");

                foreach (var issueUrl in issueUrls.Where(issueUrl => !string.IsNullOrEmpty(issueUrl)))
                {
                    var issueId = GetIdFromUrl(issueUrl);
                    if (issueId.HasValue)
                    {
                        commit.Issues.Add(Tuple.Create(issueId.Value, issueUrl));
                    }
                }

                static int? GetIdFromUrl(string url)
                {
                    var contents = url.Split('/');
                    if (contents.Any())
                    {
                        var idStr = contents.Last();
                        return int.Parse(idStr);
                    }

                    return null;
                }
            }

            static List<string> GetIssueLinks(
            string message,
            string issueRepository)
            {
                List<string> list = new List<string>();
                Regex urlRx = new Regex(@"((https?|ftp|file)\://|www.)[A-Za-z0-9\.\-]+(/[A-Za-z0-9\?\&\=;\+!'\(\)\*\-\._~%]*)*", RegexOptions.IgnoreCase);

                MatchCollection matches = urlRx.Matches(message);
                foreach (Match match in matches)
                {
                    var url = match.Value.ToLowerInvariant();

                    foreach (var issueRepo in issueRepository.Split(';'))
                    {
                        var repo = issueRepo.ToLowerInvariant();
                        if (url.Contains(repo) && url.Contains("issues"))
                        {
                            var regex = new Regex(@"[\D]*$");
                            if (regex.IsMatch(url))
                            {
                                url = regex.Replace(url, string.Empty);
                            }

                            list.Add(url);
                        }
                    }
                }

                return list;
            }
        }

        public static void SaveAsHtml(IList<CommitWithDetails> commits, string path)
        {
            var resultHtmlPath = Path.Combine(path, "results.html");
            Directory.CreateDirectory(path);

            if (File.Exists(resultHtmlPath))
            {
                File.Delete(resultHtmlPath);
            }

            using (var w = File.AppendText(resultHtmlPath))
            {
                w.WriteLine(
    @"<!DOCTYPE html>
<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml\"">
<head>
<meta charset=""utf-8"" />
<title>Change Log</title>
<style>
table, th, td {
    border: 1px solid black;
    border-collapse: collapse;
    padding: 15px;
    text-align: left;
}
</style>
</head>
<body>
<table style=""width:100%"">
<tr>
<th>Area</th>
<th>Pull Request</th>
<th>Issue(s)</th>
<th>CommitWithDetails</th>
<th>Author</th>
<th>CommitWithDetails Message</th>
</tr>");
                foreach (var commit in commits)
                {
                    w.WriteLine(
    @$"<tr>
<td></td>
<td>{GetHrefOrEmpty(commit.PR)}</td>
<td>{string.Join($"</br>", commit.Issues.Select(e => GetHrefOrEmpty(e)))}</td>
<td><a href=""{commit.Link}"">{commit.Sha}</a></td>
<td>{commit.Author}</td>
<td>{commit.Message}</td>
</tr> ");
                }

                w.WriteLine(
    @"</body>
</html>");
            }

            Console.WriteLine($"Saving results file: {resultHtmlPath}");

            static string GetHrefOrEmpty(Tuple<int, string>? url)
            {
                if (url == null)
                {
                    return string.Empty;
                }
                return $"<a href=\"{url.Item2}\">{url.Item1}</a>";
            }
        }

        public static void SaveAsMarkdown(IList<CommitWithDetails> commits, string path)
        {
            var resultMarkdownPath = Path.Combine(path, "results.md");
            Directory.CreateDirectory(path);

            if (File.Exists(resultMarkdownPath))
            {
                File.Delete(resultMarkdownPath);
            }

            using (var w = File.AppendText(resultMarkdownPath))
            {
                w.WriteLine(
                @"|Pull Request |Issue(s) |CommitWithDetails |Author |CommitWithDetails Message |");
                w.WriteLine("--- | --- | --- | --- | ---");
                foreach (var commit in commits)
                {
                    w.WriteLine($"| {GetMdURLOrEmpty(commit.PR)} | " +
                        $"{string.Join($"<br />", commit.Issues.Select(e => GetMdURLOrEmpty(e)))} | " +
                        $"{GetMdURLOrEmpty(new Tuple<string, string>(commit.Sha, commit.Link))} | " +
                        $"{commit.Author} | " +
                        $"{commit.Message} |");
                }
            }

            Console.WriteLine($"Saving results file: {resultMarkdownPath}");

            static string GetMdURLOrEmpty(object? url)
            {
                Tuple<string, string>? tuple = null;

                if (url is Tuple<string, string> stringTuple)
                {
                    tuple = stringTuple;
                }
                else if (url is Tuple<int, string> intTuple)
                {
                    tuple = new Tuple<string, string>(intTuple.Item1.ToString(), intTuple.Item2);
                }

                if (tuple != null)
                {
                    return $"[{tuple.Item1}]({tuple.Item2})";
                }

                return string.Empty;
            }
        }
    }
}