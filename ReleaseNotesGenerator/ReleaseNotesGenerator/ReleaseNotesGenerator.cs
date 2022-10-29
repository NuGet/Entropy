﻿using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReleaseNotesGenerator
{
    class ReleaseNotesGenerator
    {
        private const string NuGet = "nuget";
        private const string NuGetClient = "nuget.client";
        private const string Home = "home";

        private readonly GitHubClient GitHubClient;
        private readonly Options Options;

        public ReleaseNotesGenerator(Options opts)
        {
            Options = opts;
            GitHubClient = new GitHubClient(new ProductHeaderValue("nuget-release-notes-generator"));

            Credentials creds = null;
            if (!string.IsNullOrEmpty(opts.GitHubToken))
            {
                creds = new Credentials(opts.GitHubToken);
            }
            else
            {
                Dictionary<string, string> credentuals = GitCredentials.Get(new Uri("https://github.com/NuGet/Home"));
                if (credentuals?.TryGetValue("password", out string pat) == true)
                {
                    creds = new Credentials(pat);
                }
                else
                {
                    Console.WriteLine("Warning: Unable to get github token. Making unauthenticated HTTP requests, which has lower request limits.");
                }
            }
            GitHubClient.Credentials = creds;
        }

        public async Task<string> GenerateChangelog()
        {
            Dictionary<IssueType, List<Issue>> issues = await GetIssuesByType(NuGet, Home, Options.Release);

            List<PullRequest> CommunityPullRequests = null;
            if (!string.IsNullOrEmpty(Options.StartSha))
            {
                CommunityPullRequests = await GetCommunityPullRequests(GitHubClient, NuGet, NuGetClient, Options.StartSha, $"release-{Options.Release}.x");
            }
            return GenerateMarkdown(Options.Release, issues, CommunityPullRequests);
        }

        public static async Task<IList<Issue>> GetIssuesForMilestone(GitHubClient client, string org, string repo, Milestone milestone)
        {
            var shouldPrioritize = new RepositoryIssueRequest
            {
                Milestone = milestone.Number.ToString(),
                Filter = IssueFilter.All,
                State = ItemStateFilter.All,
            };

            var issuesForMilestone = await client.Issue.GetAllForRepository(org, repo, shouldPrioritize);

            return issuesForMilestone.ToList();
        }

        public static async Task<List<PullRequest>> GetCommunityPullRequests(GitHubClient gitHubClient, string orgName, string repoName, string startSha, string branchName, string endSha = null)
        {
            Console.Write($"Processing the pull requests for {branchName}, ");

            var githubBranch = await gitHubClient.Repository.Branch.Get(orgName, repoName, branchName);
            string finalSha = string.IsNullOrEmpty(endSha) ? githubBranch.Commit.Sha : endSha;

            Console.WriteLine($"starting with {startSha} and ending with {finalSha}");

            var commits = (await gitHubClient.Repository.Commit.Compare(orgName, repoName, startSha, finalSha)).Commits;

            List<PullRequest> pullRequests = new();

            foreach (var commit in commits)
            {
                var assumedId = GetPRId(commit.Commit.Message);
                try
                {
                    var pullRequest = await gitHubClient.Repository.PullRequest.Get(orgName, repoName, assumedId);
                    var isCommunity = pullRequest.Labels.Any(e => e.Name == IssueLabels.Community);
                    if (isCommunity)
                    {
                        pullRequests.Add(pullRequest);
                    }
                }
                catch
                {
                    Console.WriteLine($"Failed retrieving the pull request for {commit.HtmlUrl}. Calculate PR Id: {assumedId}");
                }
            }

            return pullRequests;

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

        private async Task<Dictionary<IssueType, List<Issue>>> GetIssuesByType(string org, string repo, string releaseId)
        {
            var issuesByType = new Dictionary<IssueType, List<Issue>>();

            Milestone relevantMilestone = await FindMatchingMilestone(releaseId, org, repo);

            var issueList = await GetIssuesForMilestone(GitHubClient, org, repo, relevantMilestone);

            foreach (Issue issue in issueList)
            {
                bool issueFixed = true;
                bool hidden = false;
                IssueType issueType = IssueType.None;
                bool epicLabel = false;
                bool regressionDuringThisVersion = false;
                bool engImproveOrDocs = false;

                if (issue.State == ItemState.Open)
                {
                    issueType = IssueType.StillOpen;
                }
                else
                {
                    foreach (var label in issue.Labels)
                    {
                        if (label.Name.Contains(IssueLabels.ClosedPrefix))
                        {
                            issueFixed = false;
                        }

                        if (label.Name == IssueLabels.RegressionDuringThisVersion)
                        {
                            regressionDuringThisVersion = true;
                            hidden = true;
                        }

                        if (label.Name == IssueLabels.EngImprovement || label.Name == IssueLabels.Test || label.Name == IssueLabels.Docs)
                        {
                            engImproveOrDocs = true;
                            hidden = true;
                        }

                        if (label.Name == IssueLabels.Epic)
                        {
                            epicLabel = true;
                        }
                        else if (label.Name == IssueLabels.Feature)
                        {
                            issueType = IssueType.Feature;
                        }
                        else if (label.Name == IssueLabels.DCR)
                        {
                            issueType = IssueType.DCR;
                        }
                        else if (label.Name == IssueLabels.Bug)
                        {
                            issueType = IssueType.Bug;
                        }
                        else if (label.Name == IssueLabels.Spec)
                        {
                            issueType = IssueType.Spec;
                        }
                    }
                }

                // if an issue is an epicLabel and has a real IssueType (feature/bug/dcr),
                // then hide it... we want to show the primary epic issue only.
                if (epicLabel)
                {
                    if (issueType == IssueType.None)
                    {
                        issueType = IssueType.Feature;
                    }
                    else
                    {
                        hidden = true;
                    }
                }
                else if (issueType == IssueType.None)
                {
                    if (!(issueFixed && !regressionDuringThisVersion && !engImproveOrDocs))
                    {
                        hidden = true;
                    }
                }

                if (!hidden && issueFixed)
                {
                    if (!issuesByType.ContainsKey(issueType))
                    {
                        issuesByType.Add(issueType, new List<Issue>());
                    }

                    issuesByType[issueType].Add(issue);
                }
            }

            return issuesByType;
        }

        private async Task<Milestone> FindMatchingMilestone(string releaseId, string org, string repo)
        {
            var milestones = await GitHubClient.Issue.Milestone.GetAllForRepository(org, repo);

            var relevantMilestone = milestones.SingleOrDefault(e => e.Title.Equals(releaseId));
            if (relevantMilestone == null)
            {
                throw new Exception($"No such release: {Options.Release}");
            }

            return relevantMilestone;
        }

        private static string GenerateMarkdown(string release, Dictionary<IssueType, List<Issue>> labelSet, List<PullRequest> communityPullRequests)
        {
            var version = Version.Parse(release);
            string VSYear = version.Major == 6 ? "2022" : "<TODO: VSYear. Consider updating the tool.>";
            string VSVersion = version.Major + 11 + "." + version.Minor;
            string fullSDKVersion = "<TODO: Full SDK Version>";
            string SDKMajorMinorVersion = "<TODO: SDKMajorMinorVersionOnly";
            string GithubAlias = "<TODO: GitHubAlias>";
            string MicrosoftAlias = "<TODO: MicrosoftAlias>";
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("---");
            builder.AppendLine(string.Format("title: NuGet {0} Release Notes", release));
            builder.AppendLine(string.Format("description: Release notes for NuGet {0} including new features, bug fixes, and DCRs.", release));
            builder.AppendLine(string.Format("author: {0}", GithubAlias));
            builder.AppendLine(string.Format("ms.author: {0}", MicrosoftAlias));
            builder.AppendLine(string.Format("ms.date: {0}", DateTime.Now.ToString("d", System.Globalization.CultureInfo.GetCultureInfo("en-US"))));
            builder.AppendLine("ms.topic: conceptual");
            builder.AppendLine("---");
            builder.AppendLine();
            builder.AppendLine(string.Format("# NuGet {0} Release Notes", release));
            builder.AppendLine();
            builder.AppendLine("NuGet distribution vehicles:");
            builder.AppendLine();
            builder.AppendLine("| NuGet version | Available in Visual Studio version | Available in .NET SDK(s) |");
            builder.AppendLine("|:---|:---|:---|");
            builder.AppendLine(string.Format("| [**{0}**](https://nuget.org/downloads) |" +
                " [Visual Studio {1} version {2}](https://visualstudio.microsoft.com/downloads/) " +
                "| [{3}](https://dotnet.microsoft.com/download/dotnet-core/{4})<sup>1</sup> |",
                release, VSYear, VSVersion, fullSDKVersion, SDKMajorMinorVersion));
            builder.AppendLine();
            builder.AppendLine(string.Format("<sup>1</sup> Installed with Visual Studio {0} with.NET Core workload", VSYear));
            builder.AppendLine();
            builder.AppendLine(string.Format("## Summary: What's New in {0}", release));
            builder.AppendLine();
            OutputSection(labelSet, builder, IssueType.Feature, includeHeader: false);
            builder.AppendLine("### Issues fixed in this release");
            builder.AppendLine();
            OutputSection(labelSet, builder, IssueType.DCR);
            OutputSection(labelSet, builder, IssueType.Bug);
            builder.AppendLine("[List of commits in this release](TODO: Provide the link.)");
            builder.AppendLine();
            OutputCommunityPullRequestsSection(communityPullRequests, builder);

            foreach (var key in labelSet.Keys)
            {
                if (key != IssueType.Feature && key != IssueType.DCR && key != IssueType.Bug)
                {
                    OutputSection(labelSet, builder, key, problem: true);
                }
            }

            return builder.ToString();
        }

        private static void OutputCommunityPullRequestsSection(
            List<PullRequest> communityPullRequests,
            StringBuilder builder)
        {
            if (communityPullRequests != null)
            {
                if (communityPullRequests.Count > 0)
                {
                    AddCommunityContributionsHeader(builder);
                    builder.AppendLine("Thank you to all the contributors who helped make this NuGet release awesome!");
                    builder.AppendLine();

                    var contributors = communityPullRequests.GroupBy(e => e.User).OrderBy(e => e.Count());
                    foreach (var contribution in contributors)
                    {
                        builder.AppendLine($"* [{contribution.Key.Login}]({contribution.Key.HtmlUrl})");
                        foreach (var PR in contribution)
                        {
                            builder.AppendLine($"  * [{PR.Number}]({PR.HtmlUrl}) {PR.Title}");
                        }
                    }
                }
            }
            else
            {
                AddCommunityContributionsHeader(builder);
                builder.AppendLine("TODO: The automatic generation of the release notes did not try to generate the community contributions. " +
                    "Either rerun the tool with the community contributions option or add the contributors manually. You may delete this section if there were not community contributions.");
            }

            static void AddCommunityContributionsHeader(StringBuilder builder)
            {
                builder.AppendLine("### Community contributions");
                builder.AppendLine();
            }
        }

        private static void OutputSection(
            Dictionary<IssueType, List<Issue>> labelSet,
            StringBuilder builder,
            IssueType key,
            bool includeHeader = true,
            bool problem = false)
        {
            if (labelSet.TryGetValue(key, out List<Issue> issues))
            {
                if (includeHeader)
                {
                    var issueTypeString = (problem ? "TODO: Issues that could not be categorized. Make sure the issue has the correct milestone (if required) or an appropriate Type label - " : string.Empty) + key.ToString();
                    builder.AppendLine(string.Format("**{0}s:**", issueTypeString));
                    builder.AppendLine();
                }

                foreach (var issue in issues)
                {
                    builder.AppendLine("* " + issue.Title + " - " + "[#" + issue.Number + "](" + issue.HtmlUrl + ")");
                    builder.AppendLine();
                }
            }
        }
    }
}
