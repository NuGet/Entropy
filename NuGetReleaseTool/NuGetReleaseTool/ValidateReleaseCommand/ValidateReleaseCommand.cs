using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGetReleaseTool.GenerateInsertionChangelogCommand;
using Octokit;
using Repository = NuGet.Protocol.Core.Types.Repository;

namespace NuGetReleaseTool.ValidateReleaseCommand
{
    public class ValidateReleaseCommand
    {
        private readonly GitHubClient GitHubClient;
        private readonly ValidateReleaseCommandOptions Options;
        private readonly HttpClient HttpClient;

        private string NuGetCommandlinePackageId = "NuGet.CommandLine";

        List<string> CorePackagesList = new List<string>() {
            "NuGet.Indexing",
            "NuGet.Build.Tasks.Console",
            "NuGet.Build.Tasks.Pack",
            "NuGet.Build.Tasks",
            "NuGet.CommandLine.XPlat",
            "NuGet.Commands",
            "NuGet.Common",
            "NuGet.Configuration",
            "NuGet.Credentials",
            "NuGet.DependencyResolver.Core",
            "NuGet.Frameworks",
            "NuGet.LibraryModel",
            "NuGet.Localization",
            "NuGet.PackageManagement",
            "NuGet.Packaging.Core",
            "NuGet.Packaging",
            "NuGet.ProjectModel",
            "NuGet.Protocol",
            "NuGet.Resolver",
            "NuGet.Versioning" };

        List<string> AllVSPackagesList = new()
        {
            "NuGet.VisualStudio.Contracts",
            "NuGet.VisualStudio",
        };

        public ValidateReleaseCommand(ValidateReleaseCommandOptions opts, GitHubClient gitHubClient)
        {
            Options = opts;
            GitHubClient = gitHubClient;
            HttpClient = new HttpClient();
        }

        public async Task<int> RunAsync()
        {
            Console.WriteLine("|Section | Status | Notes |");
            Console.WriteLine("|--------|--------|-------|");
            WriteResultLine("Release notes", await ValidateReleaseNotesAsync());
            WriteResultLine("Documentation readiness", await ValidateDocumentationReadinessAsync());
            WriteResultLine("SDK packages", await ValidateNuGetSDKPackagesAsync());
            WriteResultLine("NuGet.exe", await ValidateNuGetExeAsync());
            return 0;
        }

        private static void WriteResultLine(string section, (Status, string) result)
        {
            Console.WriteLine($"| {section} | {result.Item1} | {result.Item2} |");
        }

        private async Task<(Status, string)> ValidateNuGetSDKPackagesAsync()
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;
            SourceCacheContext cache = new();
            SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            var packagesMissingList = new List<string>() { };
            var expectedVersion = NuGetVersion.Parse(Options.Release);
            var expectedVsPackageVersion = new NuGetVersion(expectedVersion.Major + 11, expectedVersion.Minor, expectedVersion.Patch, expectedVersion.Revision, expectedVersion.Release, expectedVersion.Metadata);

            int expectedPackagesCount = CorePackagesList.Count + AllVSPackagesList.Count;

            foreach (var package in CorePackagesList)
            {
                if (!await resource.DoesPackageExistAsync(
                    package,
                    expectedVersion,
                    cache,
                    logger,
                    cancellationToken))
                {
                    packagesMissingList.Add(package);

                }
            }

            foreach (var package in AllVSPackagesList)
            {
                if (!await resource.DoesPackageExistAsync(
                    package,
                    expectedVsPackageVersion,
                    cache,
                    logger,
                    cancellationToken))
                {
                    packagesMissingList.Add(package);

                }
            }

            if (packagesMissingList.Count == 0)
            {
                return (Status.Completed, string.Empty);
            }
            else if (packagesMissingList.Count == expectedPackagesCount)
            {
                return (Status.NotStarted, string.Join(", ", packagesMissingList) + " are not uploaded.");
            }
            else
            {
                return (Status.InProgress, string.Join(", ", packagesMissingList) + " were expected to be available, but are not.");
            }
        }

        private async Task<(Status, string)> ValidateNuGetExeAsync()
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;
            SourceCacheContext cache = new();
            SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();
            var expectedVersion = NuGetVersion.Parse(Options.Release);
            if (await resource.DoesPackageExistAsync(
                    NuGetCommandlinePackageId,
                    expectedVersion,
                    cache,
                    logger,
                    cancellationToken))
            {
                return (Status.Completed, $"{expectedVersion} is on NuGet.org, and considered blessed");
            }

            var expectedNuGetExeUrl = $"https://dist.nuget.org/win-x86-commandline/v{expectedVersion.ToNormalizedString()}/nuget.exe";
            if (await UrlExistsAsync(HttpClient, expectedNuGetExeUrl))
            {
                return (Status.InProgress, $"{expectedVersion} is on NuGet.org, but no NuGet.Commandline package has been published and as such is not blessed.");

            }
            return (Status.NotStarted, "Not started");
        }

        private async Task<(Status, string)> ValidateDocumentationReadinessAsync()
        {
            // For a given ID/branch, get all of the linked documentation PRs that are still open.
            string[] issueRepositories = new string[] { $"{Constants.NuGet}/{Constants.DocsRepo}" };

            var githubCommits = await Helpers.GetCommitsForRelease(GitHubClient, Options.Release, Options.EndCommit);

            List<CommitWithDetails> commits = await Helpers.GetCommitDetails(GitHubClient, Constants.NuGet, Constants.NuGetClient, issueRepositories, githubCommits);

            var urls = new HashSet<string>();

            foreach (var commit in commits)
            {
                foreach (var issue in commit.Issues)
                {
                    var ghIssue = await GitHubClient.Issue.Get(Constants.NuGet, Constants.DocsRepo, issue.Item1);
                    if (ghIssue.State == ItemState.Open)
                    {
                        urls.Add(issue.Item2);
                    }
                }
            }
            if (urls.Count > 0)
            {
                return (Status.InProgress, $"Issues linked in PRs of commits that are part of the release: {string.Join(", ", urls)}");
            }
            return (Status.Completed, "No open issues");
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
                return (Status.InProgress, $"The docs repo has the release notes, but they are not published to the repo yet. {releaseNotesGHUrl}");
            }
            else
            {
                var allOpenPullRequests = await GitHubClient.PullRequest.GetAllForRepository(Constants.NuGet, Constants.DocsRepo);
                string? docsPR = null;
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
}