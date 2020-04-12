using Azure;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ZenHub;
using ZenHub.Models;



namespace UpdatePipelineLabel
{
    class LabelUpdateHelper
    {
        private const string WorkspaceName = "NuGet Client Team";
        private readonly GitHubClient GitHubClient;
        private readonly ZenHubClient ZenHubClient;

        private string repoName;
        private string githubToken;
        private string zenhubToken;
        private int issueNumberFrom;
        private int issueNumberTo;

        //If specify a range in the command, then only run issues having issue numbers in this range.
        //If range is not specified, run all the issues in the repository.
        bool processAll;

        //private SummaryReport summaryReport;

        public LabelUpdateHelper(Options options)
        {
            repoName = options.Repo;
            githubToken = options.GitHubToken;
            zenhubToken = options.ZenHubToken;

            //only run issues which is in the range, if the totall issue number exceed the rate limiting https://developer.github.com/v3/#rate-limiting
            issueNumberFrom = options.IssueNumFrom;
            issueNumberTo = options.IssueNumTo;

            //if issueNumberTo is not specified in the command, or the "from" is larger than "to" , then process all the issues
            if (issueNumberTo == 0 || (issueNumberFrom > issueNumberTo))
            {
                processAll = true;
            }

            GitHubClient = new GitHubClient(new ProductHeaderValue("nuget-update-pipeline-label"));
            var creds = new Credentials(githubToken);
            GitHubClient.Credentials = creds;
            ZenHubClient = new ZenHubClient(zenhubToken);

            //summaryReport = new SummaryReport();
        }

        public async Task UpdateLabel()
        {
            Dictionary<int, string> issueLabelMap = await GernerateIssueLabelMap();

            string[] repoParts = repoName.Split('/');

            List<string> pipelineLabels = new List<string>();
            pipelineLabels.Add(IssueLabels.NewIssues);
            pipelineLabels.Add(IssueLabels.Icebox);
            pipelineLabels.Add(IssueLabels.Backlog);
            pipelineLabels.Add(IssueLabels.InProgress);
            pipelineLabels.Add(IssueLabels.InReview);
            pipelineLabels.Add(IssueLabels.Validating);

            var issuesForRepo = await GitHubClient.Issue.GetAllForRepository(repoParts[0], repoParts[1]);

            int updatedCount = 0;
            int errorCount = 0;
            foreach (var issue in issuesForRepo)
            {
                //only test for issues which has the issue number in the range
                if ((issue.Number < issueNumberFrom || issue.Number > issueNumberTo) && !processAll)
                {
                    continue;
                }

                List<string> toBeRemovedLabels = new List<string>();
                List<string> toBeAddedLabels = new List<string>();

                string rightLabel;

                //if the issue could not be found from zenhub's any pipeline, write an exception message and skip this issue.
                if (!issueLabelMap.TryGetValue(issue.Number, out rightLabel))
                {
                    errorCount++;
                    Console.WriteLine($"Exception : Issue {issue.HtmlUrl} could not be found in any pipeline in ZenHub");
                }
                else
                {
                    if (issue.Labels == null)
                    {
                        toBeAddedLabels.Add(rightLabel);
                    }
                    else
                    {
                        if (!issue.Labels.Any(e => e.Name.Equals(rightLabel)))
                        {
                            toBeAddedLabels.Add(rightLabel);
                        }

                        foreach (var pipelineLabel in pipelineLabels)
                        {
                            //if the issue has a wrong pipeline label, add it to remove list
                            if (!pipelineLabel.Equals(rightLabel) & issue.Labels.Any(e => e.Name.Equals(pipelineLabel)))
                            {
                                toBeRemovedLabels.Add(pipelineLabel);
                            }
                        }
                    }
                }

                //update labels
                if (toBeAddedLabels.Count != 0 || toBeRemovedLabels.Count != 0)
                {
                    var issueUpdate = issue.ToUpdate();

                    foreach (var label in toBeAddedLabels)
                    {
                        issueUpdate.AddLabel(label);
                    }

                    foreach (var label in toBeRemovedLabels)
                    {
                        issueUpdate.RemoveLabel(label);
                    }

                    try
                    {
                        await GitHubClient.Issue.Update(repoParts[0], repoParts[1], issue.Number, issueUpdate);
                        Console.WriteLine($"Updated : issue {issue.HtmlUrl} ");
                        updatedCount++;

                        //According to https://developer.github.com/v3/guides/best-practices-for-integrators/#dealing-with-abuse-rate-limits
                        //If you're making a large number of POST, PATCH, PUT, or DELETE requests for a single user or client ID, wait at least one second between each request.
                        System.Threading.Thread.Sleep(1000);
                    }
                    catch (Exception e)
                    {
                        //if updating label for issue failed on github , write an exception message and skip this issue.
                        errorCount++;
                        Console.WriteLine($"Exception : Issue {issue.HtmlUrl} could not update label on GitHub, exception is : {e.Message}");
                    }
                }
            }
            Console.WriteLine($"\nProcess finished!  Total updated {updatedCount} issues, total error {errorCount}");

            return;
        }

        private async Task<Dictionary<int, string>> GernerateIssueLabelMap()
        {
            Dictionary<int, string> issueLabelMap = new Dictionary<int, string>();
            string[] repoParts = repoName.Split('/');
            Repository repo = await GitHubClient.Repository.Get(repoParts[0], repoParts[1]);

            ZenHubRepositoryClient repoClient = ZenHubClient.GetRepositoryClient(repo.Id);
            {
                Response<Workspace[]> workSpace = await repoClient.GetWorkspacesAsync();
                foreach (var workspace in workSpace.Value)
                {
                    if (workspace.Name.Equals(WorkspaceName))
                    {
                        try
                        {
                            Response<ZenHubBoard> zenHubBoard = await repoClient.GetZenHubBoardAsync(workspace.Id);

                            foreach (var pipeline in zenHubBoard.Value.Pipelines)
                            {
                                string pipelineLabel = $"Pipeline: {pipeline.Name}";
                                foreach (IssueDetails issue in pipeline.Issues)
                                {
                                    issueLabelMap.Add(issue.IssueNumber, pipelineLabel);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("If the below exception is about parsing, there may be a float estimation in some issues. \n" +
                                              "Find the one and change it to an int");
                            throw;
                        }

                    }
                }
            }
            return issueLabelMap;
        }
    }
}

