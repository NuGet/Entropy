using Azure;
using Octokit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ZenHub;
using ZenHub.Models;

namespace ChangelogGenerator
{
    class ChangelogGenerator
    {
        private readonly GitHubClient GitHubClient;
        private readonly ZenHubClient ZenHubClient;
        private readonly Options Options;

        public ChangelogGenerator(Options opts)
        {
            Options = opts;
            GitHubClient = new GitHubClient(new ProductHeaderValue("nuget-changelog-generator"));
            var creds = new Credentials(Options.GitHubToken);
            GitHubClient.Credentials = creds;
            ZenHubClient = new ZenHubClient(Options.ZenHubToken);
        }

        public async Task<string> GenerateChangelog()
        {
            string releaseId = await GetReleaseId();
            Dictionary<IssueType, List<Issue>> issues = await GetIssuesByType(releaseId);
            return GenerateMarkdown(releaseId, issues);
        }

        private async Task<string> GetReleaseId()
        {
            string[] repoParts = Options.Repo.Split('/');
            Repository repo = await GitHubClient.Repository.Get(repoParts[0], repoParts[1]);
            ZenHubRepositoryClient repoClient = ZenHubClient.GetRepositoryClient(repo.Id);

            Response<ReleaseReport[]> releases = await repoClient.GetReleaseReportsAsync();

            string releaseId = string.Empty;
            foreach (var release in releases.Value)
            {
                if (release.Title == Options.Release)
                {
                    releaseId = release.ReleaseId;
                    break;
                }
            }

            if (releaseId == string.Empty)
            {
                throw new Exception($"No such release: {Options.Release}");
            }
            return releaseId;
        }

        private async Task<Dictionary<IssueType, List<Issue>>> GetIssuesByType(string releaseId)
        {
            Dictionary<IssueType, List<Issue>> issuesByType = new Dictionary<IssueType, List<Issue>>();

            ZenHubReleaseClient releaseClient = ZenHubClient.GetReleaseClient(releaseId);
            IssueDetails[] zenHubIssueList = (await releaseClient.GetIssuesAsync()).Value;
            string[] repoParts = Options.Repo.Split('/');
            foreach (IssueDetails details in zenHubIssueList)
            {
                Issue issue = await GitHubClient.Issue.Get(repoParts[0], repoParts[1], details.IssueNumber);
                bool issueFixed = true;
                bool hidden = false;
                IssueType issueType = IssueType.None;
                bool epicLabel = false;
                bool regressionDuringThisVersion = false;
                bool engineeringImprovement = false;
                string requiredLabel = Options.RequiredLabel?.ToLower();
                bool foundRequiredLabel = string.IsNullOrEmpty(requiredLabel);

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

                    if (label.Name == IssueLabels.EngImprovement)
                    {
                        engineeringImprovement = true;
                        hidden = true;
                    }

                    if (!foundRequiredLabel && label.Name.ToLower() == requiredLabel)
                    {
                        foundRequiredLabel = true;
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

                if (!foundRequiredLabel)
                {
                    hidden = true;
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
                    if (!(issueFixed && !regressionDuringThisVersion && !engineeringImprovement))
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

        private string GenerateMarkdown(string releaseId, Dictionary<IssueType, List<Issue>> labelSet)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# " + Options.Release + (!string.IsNullOrEmpty(Options.RequiredLabel) ? "-" + Options.RequiredLabel.ToLower() : "") + " Release Notes");
            builder.AppendLine();
            builder.AppendLine("[Full Changelog]" + "(\"\")");
            builder.AppendLine();
            builder.AppendLine("[Issues List]"
                + "("
                + "https://app.zenhub.com/workspaces/nuget-client-team-55aec9a240305cf007585881/reports/release?release="
                + releaseId
                + ")");
            builder.AppendLine();
            foreach (var key in labelSet.Keys)
            {
                var issueTypeString = key.ToString();
                builder.AppendLine("**" + issueTypeString + ":**");
                builder.AppendLine();
                foreach (var issue in labelSet[key])
                {
                    builder.AppendLine("* " + issue.Title + " - " + "[#" + issue.Number + "](" + issue.HtmlUrl + ")");
                    builder.AppendLine();
                }
            }

            return builder.ToString();


        }
    }
}
