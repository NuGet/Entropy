using CommandLine;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangelogGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Parser.Default.ParseArguments(args, options))
            {
                // consume Options instance properties
                if (options.Verbose)
                {
                    Console.WriteLine(options.Organization);
                    Console.WriteLine(options.Repo);
                    Console.WriteLine(options.Milestone);
                }
            }
            else
            {
                Console.WriteLine(options.GetUsage());
            }

            var tokenAuth = new Credentials(options.GitHubToken);
            client.Credentials = tokenAuth;
            var task = GetChangelog();
            task.Wait();
        }

        private async static Task GetChangelog()
        {
            try
            {
                RepositoryIssueRequest issueQuery;

                if (options.IncludeOpen == "Y")
                {
                    issueQuery = new RepositoryIssueRequest
                    {
                        Filter = IssueFilter.All,
                        State = ItemStateFilter.All,
                    };
                }
                else
                {
                    issueQuery = new RepositoryIssueRequest
                    {
                        Filter = IssueFilter.All,
                        State = ItemStateFilter.Closed,
                    };
                }

                int startWeek = 1;
                int endWeek = 1;
                DateTime? startDateTime = null;
                DateTime? endDateTime = null;

                if (!string.IsNullOrEmpty(options.WeekRange))
                {
                    var splitters = new char[] { ',' };
                    var weekRange = options.WeekRange.Split(splitters, StringSplitOptions.RemoveEmptyEntries);

                    switch (weekRange.Length)
                    {
                        case 1:
                            if (int.TryParse(weekRange[0], out startWeek))
                            {
                                endWeek = startWeek;
                            }
                            break;
                        case 2:
                            int.TryParse(weekRange[0], out startWeek);
                            int.TryParse(weekRange[1], out endWeek);
                            break;
                        default:
                            break;
                    }

                    var date = DateTime.Now;
                    DateTime lastFriday = date.AddDays(-(int)date.DayOfWeek - 2);
                    DateTime lastFridayEOD = new DateTime(lastFriday.Year, lastFriday.Month, lastFriday.Day, 17, 0, 0);

                    startDateTime = lastFridayEOD.AddDays(7 * startWeek);
                    endDateTime = lastFridayEOD.AddDays(7 * (endWeek + 1));
                }

                Console.WriteLine("querying github @ " + DateTime.Now.ToLongTimeString());
                var issues = await client.Issue.GetAllForRepository(options.Organization, options.Repo, issueQuery);
                Console.WriteLine($"completed retrieval of {issues.Count} issues @ {DateTime.Now.ToLongTimeString()}");
                Console.WriteLine($"filtering...");

                var IssuesList = new List<IssueInfo>();

                bool filterByMilestone = !string.IsNullOrEmpty(options.Milestone);

                foreach (var issue in issues)
                {
                    bool includeByMilestone = filterByMilestone && issue.Milestone?.Title == options.Milestone;
                    bool includeByWeekRange = issue.CreatedAt >= startDateTime && issue.CreatedAt < endDateTime;

                    if (includeByMilestone || includeByWeekRange)
                    {
                        var issueInfo = new IssueInfo();
                        issueInfo.Issue = issue;
                        issueInfo.IssueType = IssueType.None;

                        bool epicLabel = false;
                        bool regressionDuringThisVersion = false;
                        bool engineeringImprovement = false;
                        string requiredLabel = options.RequiredLabel?.ToLower();
                        bool foundRequiredLabel = string.IsNullOrEmpty(requiredLabel);

                        foreach (var label in issue.Labels)
                        {
                            if (label.Name.Contains("ClosedAs:"))
                            {
                                issueInfo.IsFix = false;
                            }

                            if (label.Name == "RegressionDuringThisVersion")
                            {
                                regressionDuringThisVersion = true;
                                issueInfo.HideFromReleaseNotes = true;
                            }

                            if (label.Name == "Area: Engineering Improvements")
                            {
                                engineeringImprovement = true;
                                issueInfo.HideFromReleaseNotes = true;
                            }

                            if (!foundRequiredLabel && label.Name.ToLower() == requiredLabel)
                            {
                                foundRequiredLabel = true;
                            }

                            switch (label.Name)
                            {
                                case "Epic":
                                    epicLabel = true;
                                    break;
                                case "Type:Feature":
                                    issueInfo.IssueType = IssueType.Feature;
                                    break;
                                case "Type:DCR":
                                    issueInfo.IssueType = IssueType.DCR;
                                    break;
                                case "Type:Bug":
                                    issueInfo.IssueType = IssueType.Bug;
                                    break;
                                case "Type:Spec":
                                    issueInfo.IssueType = IssueType.Spec;
                                    break;
                                case "Type:Doc":
                                    issueInfo.IssueType = IssueType.Doc;
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (!foundRequiredLabel)
                        {
                            issueInfo.FilterFromIssueList = true;
                        }

                        // if an issue is an epicLabel and has a real IssueType (feature/bug/dcr),
                        // then hide it... we want to show the primary epic issue only.
                        if (epicLabel)
                        {
                            if (issueInfo.IssueType == IssueType.None)
                            {
                                issueInfo.IssueType = IssueType.Feature;
                            }
                            else
                            {
                                issueInfo.HideFromReleaseNotes = true;
                            }
                        }
                        else if (issueInfo.IssueType == IssueType.None)
                        {
                            if (!(issueInfo.IsFix && !regressionDuringThisVersion && !engineeringImprovement))
                            {
                                issueInfo.HideFromReleaseNotes = true;
                            }
                        }

                        if (!issueInfo.FilterFromIssueList && !issueInfo.HideFromReleaseNotes)
                        {
                            IssuesList.Add(issueInfo);
                        }
                    }
                }

                Console.WriteLine($"completed filtering of {IssuesList.Count} issues @ {DateTime.Now.ToLongTimeString()}");

                GenerateMarkdown(IssuesList, filterByMilestone, startDateTime, endDateTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void GenerateMarkdown(List<IssueInfo> issuesList, bool isMilestoneReport, DateTime? startDateTime, DateTime? endDateTime)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("#" + options.Milestone + (!string.IsNullOrEmpty(options.RequiredLabel) ? "-" + options.RequiredLabel.ToLower() : "") + " Release Notes");
            builder.AppendLine();
            builder.AppendLine();
            builder.Append("[Full Changelog]" + "(" + changeloglink + ")");
            builder.AppendLine();
            builder.AppendLine();
            builder.Append("[Issues List]"
                + "(" + "https://github.com/"
                + options.Organization + "/"
                + options.Repo + "/"
                + "issues?q=is%3Aissue+is%3Aclosed" + (!string.IsNullOrEmpty(options.RequiredLabel) ? "+label:" + options.RequiredLabel : "") + "+milestone%3A%22"
                + options.Milestone
                + "\")");
            builder.AppendLine();
            builder.AppendLine();

            string fileName;

            if (isMilestoneReport)
            {
                var orderedIssues = from m in issuesList
                                    orderby m.IssueType, m.Issue.Id
                                    select m;
                string lastIssueTypeHeader = "";
                foreach (var issueInfo in orderedIssues)
                {
                    var issueTypeString = issueInfo.IssueType.ToString();
                    if (lastIssueTypeHeader != issueTypeString)
                    {
                        // show header for issueType
                        builder.Append("**" + issueTypeString + "s:**");
                        builder.AppendLine();
                        builder.AppendLine();
                        lastIssueTypeHeader = issueTypeString;
                    }

                    var issue = issueInfo.Issue;
                    builder.Append("* " + issue.Title + " - " + "[#" + issue.Number + "](" + issue.HtmlUrl + ")");
                    builder.AppendLine();
                    builder.AppendLine();
                }

                fileName = "Changelog-" + options.Milestone
                 + (string.IsNullOrEmpty(options.RequiredLabel) ? "" : "-" + options.RequiredLabel) + ".md";
            }
            else
            {
                var orderedIssues = from m in issuesList
                                    orderby m.Issue.Milestone?.Title, m.Issue.State.ToString(), m.IssueType, m.Issue.Id
                                    select m;
                string lastMilestoneHeader = "<<LAST-HEADER>>";
                foreach (var issueInfo in orderedIssues)
                {
                    var milestoneString = issueInfo.Issue.Milestone?.Title;
                    if (milestoneString == null) milestoneString = "None";
                    if (lastMilestoneHeader != milestoneString)
                    {
                        // show header for issueType
                        builder.Append("** Milestone: " + milestoneString + ":**");
                        builder.AppendLine();
                        builder.AppendLine();
                        lastMilestoneHeader = milestoneString;
                    }

                    var issue = issueInfo.Issue;
                    var closed = issue.State == ItemState.Closed ? " closed" : "";
                    builder.Append("* " + issue.Title + " - " + "[" + issueInfo.IssueType.ToString() + "#" + issue.Number + closed + "](" + issue.HtmlUrl + ")");
                    builder.AppendLine();
                    builder.AppendLine();
                }

                fileName = "NewIssuesFrom" + startDateTime.Value.ToShortDateString() + "-" + endDateTime.Value.ToShortDateString() + ".md";
                fileName = fileName.Replace("/", "-");
            }

            File.WriteAllText(fileName, builder.ToString());
            Console.WriteLine($"{fileName} creation complete");
        }

        static GitHubClient client = new GitHubClient(new ProductHeaderValue("Microsoft.NuGet.Tool"));
        static string title = string.Empty;
        static string changeloglink = string.Empty;
        static string issueslink = string.Empty;
        static Options options = new Options();
    }
}
