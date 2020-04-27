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

        private string RepoName;
        private string GithubToken;
        private string ZenhubToken;
        private int IssueNumberFrom;
        private int IssueNumberTo;

        //If specify a range in the command, then only run issues having issue numbers in this range.
        //If range is not specified, run all the issues in the repository.
        private bool ProcessAll;
        public LabelUpdateHelper(Options options)
        {
            RepoName = options.Repo;
            GithubToken = options.GitHubToken;
            ZenhubToken = options.ZenHubToken;

            //only run issues which is in the range, if the totall issue number exceed the rate limiting https://developer.github.com/v3/#rate-limiting
            IssueNumberFrom = options.IssueNumFrom;
            IssueNumberTo = options.IssueNumTo;

            //if IssueNumberTo is not specified in the command, or the "from" is larger than "to" , then process all the issues
            if (IssueNumberTo == 0 || (IssueNumberFrom > IssueNumberTo))
            {
                ProcessAll = true;
            }

            GitHubClient = new GitHubClient(new ProductHeaderValue("nuget-update-pipeline-label"));
            var creds = new Credentials(GithubToken);
            GitHubClient.Credentials = creds;
            ZenHubClient = new ZenHubClient(ZenhubToken);

            //summaryReport = new SummaryReport();
        }

        public async Task UpdateLabel()
        {
            Dictionary<int, string> issueLabelMap = await GernerateIssueLabelMap();

            string[] repoParts = RepoName.Split('/');

            List<string> pipelineLabels = new List<string>()

            {
                IssueLabels.NewIssues,

                IssueLabels.Icebox,

                IssueLabels.Backlog,

                IssueLabels.InProgress,

                IssueLabels.InReview,

                IssueLabels.Validating
            };


            var issuesForRepo = await GitHubClient.Issue.GetAllForRepository(repoParts[0], repoParts[1]);

            int updatedCount = 0;
            int errorCount = 0;
            foreach (var issue in issuesForRepo)
            {
                //only test for issues which has the issue number in the range
                if ((issue.Number < IssueNumberFrom || issue.Number > IssueNumberTo) && !ProcessAll)
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
            string[] repoParts = RepoName.Split('/');
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
                                string pipelineLabel = $"Pipeline:{pipeline.Name}";
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

