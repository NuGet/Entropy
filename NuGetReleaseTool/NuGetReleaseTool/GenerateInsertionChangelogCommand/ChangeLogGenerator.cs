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
            List<Commit> commits = await GetCommitDetails(gitHubClient, orgName, repoName, issueRepositories, githubCommits);

            SaveAsHtml(commits, resultPath);
            SaveAsMarkdown(commits, resultPath);
        }

        private static async Task<List<Commit>> GetCommitDetails(GitHubClient gitHubClient, string orgName, string repoName, string[] issueRepositories, IEnumerable<GitHubCommit> githubCommits)
        {
            var processedCommits = new List<Commit>();
            foreach (var ghCommit in githubCommits)
            {
                var author = ghCommit.Author?.Login ?? ghCommit.Committer?.Login ?? "";
                var commit = new Commit(ghCommit.Sha, author, $"https://github.com/{orgName}/{repoName}/commit/{ghCommit.Sha}", ghCommit.Commit.Message);
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
            var resultHtmlPath = Path.Combine(path, "results.html");
            Directory.CreateDirectory(Path.GetDirectoryName(resultHtmlPath));

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
<th>Commit</th>
<th>Author</th>
<th>Commit Message</th>
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

            static string GetHrefOrEmpty(Tuple<int, string> url)
            {
                if (url == null)
                {
                    return string.Empty;
                }
                return $"<a href=\"{url.Item2}\">{url.Item1}</a>";
            }
        }

        public static void SaveAsMarkdown(IList<Commit> commits, string path)
        {
            var resultMarkdownPath = Path.Combine(path, "results.md");
            Directory.CreateDirectory(Path.GetDirectoryName(resultMarkdownPath));

            if (File.Exists(resultMarkdownPath))
            {
                File.Delete(resultMarkdownPath);
            }

            using (var w = File.AppendText(resultMarkdownPath))
            {
                w.WriteLine(
                @"|Pull Request |Issue(s) |Commit |Author |Commit Message |");
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

            static string GetMdURLOrEmpty(object url)
            {
                Tuple<string, string> tuple = null;

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
