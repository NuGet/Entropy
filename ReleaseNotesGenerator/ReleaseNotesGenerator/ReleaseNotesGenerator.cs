using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangelogGenerator
{
    class ReleaseNotesGenerator
    {
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
            Dictionary<IssueType, List<Issue>> issues = await GetIssuesByType(Options.Release);
            return GenerateMarkdown(Options.Release, issues);
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

        private async Task<Dictionary<IssueType, List<Issue>>> GetIssuesByType(string releaseId)
        {
            var issuesByType = new Dictionary<IssueType, List<Issue>>();

            GetRepositoryDetrails(out string org, out string repo);

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

        private void GetRepositoryDetrails(out string org, out string repo)
        {
            var repoParts = Options.Repo.Split("/");
            if (repoParts.Length != 2)
            {
                throw new Exception($"Expected the repo to be 2 part, separated by `/`. Repo:{Options.Repo }, parts{string.Join("; ", repoParts)} ");
            }
            org = repoParts[0];
            repo = repoParts[1];
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

        private string GenerateMarkdown(string releaseId, Dictionary<IssueType, List<Issue>> labelSet)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("---");
            builder.AppendLine(string.Format("title: NuGet {0} Release Notes", Options.Release));
            builder.AppendLine(string.Format("description: Release notes for NuGet {0} including new features, bug fixes, and DCRs.", Options.Release));
            builder.AppendLine("author: <GithubAlias>");
            builder.AppendLine("ms.author: <MicrosoftAlias>");
            builder.AppendLine(string.Format("ms.date: {0}", DateTime.Now.ToString("d", System.Globalization.CultureInfo.GetCultureInfo("en-US"))));
            builder.AppendLine("ms.topic: conceptual");
            builder.AppendLine("---");
            builder.AppendLine();
            builder.AppendLine(string.Format("# NuGet {0} Release Notes", Options.Release));
            builder.AppendLine();
            builder.AppendLine("NuGet distribution vehicles:");
            builder.AppendLine();
            builder.AppendLine("| NuGet version | Available in Visual Studio version | Available in .NET SDK(s) |");
            builder.AppendLine("|:---|:---|:---|");
            builder.AppendLine("| [**<NuGetVersion>**](https://nuget.org/downloads) | [Visual Studio <VSYear> version <VSVersion>](https://visualstudio.microsoft.com/downloads/) | [<SDKVersion>](https://dotnet.microsoft.com/download/dotnet-core/<SDKMajorMinorVersionOnly>)<sup>1</sup> |");
            builder.AppendLine();
            builder.AppendLine("<sup>1</sup> Installed with Visual Studio <VSYear> with.NET Core workload");
            builder.AppendLine();
            builder.AppendLine(string.Format("## Summary: What's New in {0}", Options.Release));
            builder.AppendLine();
            OutputSection(labelSet, builder, IssueType.Feature, includeHeader: false);
            builder.AppendLine("### Issues fixed in this release");
            builder.AppendLine();
            OutputSection(labelSet, builder, IssueType.DCR);
            OutputSection(labelSet, builder, IssueType.Bug);

            foreach (var key in labelSet.Keys)
            {
                if (key != IssueType.Feature && key != IssueType.DCR && key != IssueType.Bug)
                {
                    // these sections shouldn't exist. tweak the issues in github until these issues move to Feature, Bug, DCR, or go away.
                    OutputSection(labelSet, builder, key, problem: true);
                }
            }

            return builder.ToString();
        }

        private static void OutputSection(
            Dictionary<IssueType, List<Issue>> labelSet,
            StringBuilder builder,
            IssueType key,
            bool includeHeader = true,
            bool problem = false)
        {

            List<Issue> issues = null;
            bool hasIssues = labelSet.TryGetValue(key, out issues);

            if (hasIssues)
            {
                if (includeHeader)
                {
                    var issueTypeString = key.ToString();
                    builder.AppendLine(string.Format("**{0}s:**", issueTypeString));
                    builder.AppendLine();
                }

                foreach (var issue in labelSet[key])
                {
                    builder.AppendLine("* " + issue.Title + " - " + "[#" + issue.Number + "](" + issue.HtmlUrl + ")");
                    builder.AppendLine();
                }
            }
        }
    }
}
