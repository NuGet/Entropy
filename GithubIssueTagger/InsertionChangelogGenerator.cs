using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GithubIssueTagger
{
    public class InsertionChangelogGenerator
    {
        public static async Task GenerateInsertionChangelogForNuGetClient(GitHubClient gitHubClient, string startSha = "58795a41a3c8c9d63801fb5072a2e68e48d7507e", string branchName = "dev" )
        {
            // Adjust the sha/repo
            var orgName = "nuget";
            var repoName = "nuget.client";
            string[] issueRepositories = new string[] { "NuGet/Home", "NuGet/Client.Engineering" };
            var resultHtmlPath = Path.Combine(Path.GetDirectoryName(typeof(InsertionChangelogGenerator).Assembly.Location), "results.html");

            var githubBranch = await gitHubClient.Repository.Branch.Get(orgName, repoName, branchName);
            var githubCommits = (await gitHubClient.Repository.Commit.Compare(orgName, repoName, startSha, githubBranch.Commit.Sha)).Commits.Reverse();
            List<Commit> commits = await GetCommitDetails(gitHubClient, orgName, repoName, issueRepositories, githubCommits);

            SaveAsHtml(commits, resultHtmlPath);
        }

        private static async Task<List<Commit>> GetCommitDetails(GitHubClient gitHubClient, string orgName, string repoName, string[] issueRepositories, IEnumerable<GitHubCommit> githubCommits)
        {
            var processedCommits = new List<Commit>();
            foreach (var ghCommit in githubCommits)
            {
                var commit = new Commit(ghCommit.Sha, ghCommit.Author.Login, $"https://github.com/{orgName}/{repoName}/commit/{ghCommit.Sha}", ghCommit.Commit.Message);
                var id = GetPRId(ghCommit.Commit.Message);
                string pullRequestbody = string.Empty;
                if (id != -1)
                {
                    commit.PR = new Tuple<int, string>(id, @"https://github.com/nuget/nuget.client/pull/" + id);
                    pullRequestbody = (await gitHubClient.Repository.PullRequest.Get(orgName, repoName, id)).Body;
                }

                UpdateCommitIssuesFromText(commit, pullRequestbody, issueRepositories);
                processedCommits.Add(commit);
            }

            return processedCommits;

            static int GetPRId(string message)
            {
                foreach (Match match in new Regex(@"(#\d+)", RegexOptions.IgnoreCase).Matches(message))
                {
                    var pullRequestIdText = match.Value.Substring(1, match.Length - 1);
                    int.TryParse(pullRequestIdText, out int prId);
                    return prId;
                }

                return -1;
            }
        }

        public static void UpdateCommitIssuesFromText(
            Commit commit,
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

        public static List<string> GetIssueLinks(
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

        public static void SaveAsHtml(IList<Commit> commits, string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (var w = File.AppendText(path))
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
<th>Commit</th>
<th>Author</th>
<th>Commit Message</th>
<tr>");
                foreach (var commit in commits)
                {
                    w.WriteLine(
@$"<tr>
<td></td>
<td>{GetHrefOrEmpty(commit.PR)}</td>
<td>{string.Join($"</br>", commit.Issues.Select(e => GetHrefOrEmpty(e)))}</td>
<td><a href=""{commit.Link}"">{commit.Sha}</a></td>
<td>{commit.Author}</ td >
<td>{commit.Message}</ td >
<tr> ");
                }

                w.WriteLine(
@"</body>
</html>");
            }

            Console.WriteLine($"Saving results file: {path}");

            static string GetHrefOrEmpty(Tuple<int, string> url)
            {
                if (url == null)
                {
                    return string.Empty;
                }
                return $"<a href=\"{url.Item2}\">{url.Item1}</a>";
            }
        }

        public class Commit
        {
            public string Sha { get; }
            public string Author { get; }
            public string Link { get; }
            public string Message { get; }

            public ISet<Tuple<int, string>> Issues { get; }

            public Tuple<int, string> PR { get; set; }
            
            public Commit(string sha, string author, string link, string message)
            {
                Sha = sha;
                Author = author;
                Link = link;
                Message = message?
                            .Replace("\r", " ")
                            .Replace("\n", " ");
                Issues = new HashSet<Tuple<int, string>>();
            }
        }
    }
}
