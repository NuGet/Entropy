using Octokit;

namespace NuGetReleaseTool.ValidateReleaseCommand
{
    public class ValidateReleaseCommand
    {
        private const string NuGet = "nuget";
        private const string NuGetClient = "nuget.client";
        private const string Home = "home";
        private const string Docs = "docs.microsoft.com-nuget";

        private readonly GitHubClient GitHubClient;
        private readonly ValidateReleaseCommandOptions Options;
        private readonly HttpClient HttpClient;

        public ValidateReleaseCommand(ValidateReleaseCommandOptions opts, GitHubClient gitHubClient)
        {
            Options = opts;
            GitHubClient = gitHubClient;
            HttpClient = new HttpClient();
        }

        public async Task<int> RunAsync()
        {
            (Status, string) releaseNotesResult = await ValidateReleaseNotesAsync();
            ValidateDocumentationReadiness();
            ValidateNuGetExe();
            ValidateNuGetSDKPackages();
            return 0;
        }

        private void ValidateNuGetSDKPackages()
        {
        }

        private void ValidateNuGetExe()
        {
        }

        private void ValidateDocumentationReadiness()
        {
        }

        private async Task<(Status, string)> ValidateReleaseNotesAsync()
        {
            var releaseNotesUrl = $"https://learn.microsoft.com/en-us/nuget/release-notes/nuget-{Options.Release}";
            var releaseNotesGHUrl = $"https://github.com/NuGet/docs.microsoft.com-nuget/blob/main/docs/release-notes/NuGet-{Options.Release}.md";
            if (await UrlExistsAsync(HttpClient, releaseNotesUrl))
            {
                return (Status.Completed, releaseNotesUrl);
            }
            else if (await UrlExistsAsync(HttpClient, releaseNotesGHUrl))
            {
                return (Status.InProgress, $"The docs repo has the release notes, but they are not published to the repo yet. {releaseNotesGHUrl}".);
            }
            else
            {
                var allOpenPullRequests = await GitHubClient.PullRequest.GetAllForRepository(NuGet, Docs);
                string docsPR = null;
                foreach (var pullRequests in allOpenPullRequests)
                {
                    if (pullRequests.Title.Contains(Options.Release))
                    {
                        docsPR = pullRequests.HtmlUrl;
                    }
                }

                if (docsPR != null)
                {
                    return (Status.InProgress, $"The docs repo has a PR for the release notes. {docsPR}");

                }
                else
                {
                    return (Status.NotStarted, $"{releaseNotesUrl} and {releaseNotesGHUrl} do not exist. No active PR detected");
                }
            }
        }

        private static async Task<bool> UrlExistsAsync(HttpClient httpClient, string url)
        {
            try
            {
                var result = await httpClient.GetAsync(url);
                return result.IsSuccessStatusCode;
            }
            catch (Exception) { }
            return false;
        }
    }

    public enum Status
    {
        Completed,
        InProgress,
        NotStarted
    }
}
1