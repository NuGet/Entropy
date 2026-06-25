using System;
using System.CommandLine;
using GithubIssueTagger.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace GithubIssueTagger.Reports.IceBox
{
    internal class IceBoxReportCommandFactory : ICommandFactory
    {
        public Command CreateCommand(Type type, GitHubPatBinder patBinder)
        {
            var command = new Command("IceBox");
            command.Description = "Check for issues with a label that exceed a count of upvotes since the label was added.";

            var ownerOption = new Option<string>("--owner");
            ownerOption.AddAlias("-o");
            ownerOption.Description = "GitHub owner (org or user) of the repo.";
            ownerOption.SetDefaultValue("NuGet");
            command.Add(ownerOption);

            var repoOption = new Option<string>("--repo");
            repoOption.AddAlias("-r");
            repoOption.Description = "Repo to search issues in.";
            repoOption.SetDefaultValue("Home");
            command.Add(repoOption);

            var labelOption = new Option<string>("--label");
            labelOption.AddAlias("-l");
            labelOption.Description = "Which label to filter issues by.";
            labelOption.SetDefaultValue("pipeline:IceBox");
            command.Add(labelOption);

            var upvotesOption = new Option<int>("--upvotes");
            upvotesOption.AddAlias("-u");
            upvotesOption.Description = "Number of upvotes required to meet threshold.";
            upvotesOption.SetDefaultValue(5);
            command.Add(upvotesOption);

            var addOption = new Option<string>("--add");
            addOption.AddAlias("-a");
            addOption.Description = "Label to add on issues which meet or exceed the upvote threshold. When not specified, no action is taken.";
            command.Add(addOption);

            var issueOption = new Option<int[]>("--issue");
            issueOption.AddAlias("-i");
            issueOption.Description = "Specific issue number(s) to process instead of every issue with the label. Can be repeated or given multiple values. Useful for debugging.";
            issueOption.AllowMultipleArgumentsPerToken = true;
            command.Add(issueOption);

            var verboseOption = new Option<bool>("--verbose");
            verboseOption.AddAlias("-v");
            verboseOption.Description = "Output the cutoff date and upvote count for every processed issue.";
            command.Add(verboseOption);

            command.SetHandler(async
                (GitHubPat pat,
                string owner,
                string repo,
                string label,
                int upvotes,
                string add,
                int[] issues,
                bool verbose) =>
            {
                var serviceProvider = new ServiceCollection()
                    .AddGithubIssueTagger(pat)
                    .BuildServiceProvider();

                var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
                using (scopeFactory.CreateScope())
                {
                    var report = serviceProvider.GetRequiredService<IceBoxReport>();
                    await report.RunAsync(owner, repo, label, upvotes, add, issues, verbose);
                }
            }, patBinder, ownerOption, repoOption, labelOption, upvotesOption, addOption, issueOption, verboseOption);

            return command;
        }
    }
}
