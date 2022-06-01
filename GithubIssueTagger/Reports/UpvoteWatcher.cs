using Microsoft.Extensions.DependencyInjection;
using Octokit;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports
{
    [CommandFactory(typeof(UpvoteWatcherCommandFactory))]
    internal class UpvoteWatcher : IReport
    {
        private GitHubClient _client;

        public UpvoteWatcher(GitHubClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task RunAsync()
        {
            Console.WriteLine("This command cannot run in interactive mode.");
            return Task.CompletedTask;
        }

        public async Task RunAsync(string owner, string repo, string label, int min, string add)
        {
            var openIssues = await GetIssues(owner, repo, label);

            foreach (var openIssue in openIssues)
            {
                if (openIssue.Reactions.Plus1 >= min)
                {
                    Console.WriteLine("Issue ({0}) {1} has {2} upvotes.", openIssue.Number, openIssue.Title, openIssue.Reactions.Plus1);

                    if (add != null && !openIssue.Labels.Any(l => StringComparer.OrdinalIgnoreCase.Equals(l.Name, add)))
                    {
                        // updating labels works by listing all the labels that the issue should contain after the update is finished.
                        // Therefore, step 1 is to add all existing labels
                        var updates = new IssueUpdate();
                        foreach (var existingLabel in openIssue.Labels)
                        {
                            updates.AddLabel(existingLabel.Name);
                        }

                        // Now we want to remove the search label, and add the new label
                        updates.RemoveLabel(label);
                        updates.AddLabel(add);

                        await _client.Issue.Update(owner, repo, openIssue.Number, updates);
                    }
                }
            }
        }

        private async Task<IReadOnlyList<Issue>> GetIssues(string owner, string repo, string label)
        {
            var apiOptions = new ApiOptions
            {
                PageCount = 100,
                PageSize = 100
            };
            var request = new RepositoryIssueRequest();
            request.Labels.Add(label);
            IReadOnlyList<Issue> response = await _client.Issue.GetAllForRepository(owner, repo, request, apiOptions);

            return response;
        }

        private class UpvoteWatcherCommandFactory : ICommandFactory
        {
            public Command CreateCommand(Type type, GitHubClientBinder clientBinder)
            {
                var command = new Command(nameof(UpvoteWatcher));
                command.Description = "Find issues with a label and number of upvotes, and optionally add another label";

                var owner = new Option<string>("--owner");
                owner.AddAlias("-o");
                owner.Description = "GitHub organization or user owning the repo";
                owner.IsRequired = true;
                command.AddOption(owner);

                var repo = new Option<string>("--repo");
                repo.AddAlias("-r");
                repo.Description = "GitHub repo to check";
                repo.IsRequired = true;
                command.AddOption(repo);

                var label = new Option<string>("--label");
                label.AddAlias("-l");
                label.Description = "Label to search";
                label.IsRequired = true;
                command.AddOption(label);

                var min = new Option<int>("--min");
                min.AddAlias("-m");
                min.Description = "Min upvotes required for issues";
                min.IsRequired = true;
                command.AddOption(min);

                var add = new Option<string>("--add");
                add.AddAlias("-a");
                add.Description = "Label to add to issues matching search label and min upvotes";
                command.AddOption(add);

                Func<GitHubClient, string, string, string, int, string, Task> func = RunAsync;
                command.SetHandler(func,
                    clientBinder,
                    owner,
                    repo,
                    label,
                    min,
                    add);

                return command;
            }

            public async Task RunAsync(GitHubClient client, string owner, string repo, string label, int min, string add)
            {
                var serviceProvider = new ServiceCollection()
                    .AddGithubIssueTagger(client)
                    .BuildServiceProvider();

                var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
                using (var scope = scopeFactory.CreateScope())
                {
                    var report = serviceProvider.GetRequiredService<UpvoteWatcher>();
                    await report.RunAsync(owner, repo, label, min, add);
                }
            }
        }
    }
}
