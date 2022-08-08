using Microsoft.Extensions.DependencyInjection;
using Octokit;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports.CiInitiative
{
    [CommandFactory(typeof(CiInitiativeCommandFactory))]
    internal class CiInitiativeReport : IReport
    {
        private readonly GitHubClient _client;

        public CiInitiativeReport(GitHubClient client)
        {
            _client = client;
        }

        public Task RunAsync()
        {
            Console.WriteLine("This report cannot run in interactive mode");
            return Task.CompletedTask;
        }

        public async Task RunAsync(string sprintName, OutputFormat outputFormat)
        {
            var sprint = SprintUtilities.GetSprintStartAndEnd(sprintName);
            IReadOnlyList<Issue> issues = await GetCompletedCiIssuesBetweenDatesAsync(sprint.start, sprint.end);

            var outputter = GetOutputter(outputFormat);
            outputter(issues);
        }

        private async Task<IReadOnlyList<Issue>> GetCompletedCiIssuesBetweenDatesAsync(DateOnly start, DateOnly end)
        {
            var tasks = new Task<IReadOnlyList<Issue>>[]
            {
                GetClosedCiIssuesAsync("Home", start, end),
                GetClosedCiIssuesAsync("Client.Engineering", start, end)
            };

            await Task.WhenAll(tasks);

            IEnumerable<Issue> closedCiIssues = await tasks[0];
            for (int i = 1; i < tasks.Length; i++)
            {
                closedCiIssues = closedCiIssues.Concat(await tasks[i]);
            }

            List<Issue> issues = closedCiIssues
                .Where(CompletedWork)
                .OrderBy(issue => issue.ClosedAt)
                .ToList();

            return issues;

            static bool CompletedWork(Issue issue)
            {
                foreach (var label in issue.Labels)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals("Resolution:WontFix", label.Name)
                        || StringComparer.OrdinalIgnoreCase.Equals("Resolution:Duplicate", label.Name)
                        || StringComparer.OrdinalIgnoreCase.Equals("Resolution:NotRepro", label.Name)
                        || StringComparer.OrdinalIgnoreCase.Equals("Epic", label.Name))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private async Task<IReadOnlyList<Issue>> GetClosedCiIssuesAsync(string repo, DateOnly sprintStart, DateOnly sprintEnd)
        {
            var request = new RepositoryIssueRequest()
            {
                Since = sprintStart.ToDateTime(TimeOnly.MinValue),
                State = ItemStateFilter.Closed
            };
            request.Labels.Add("CI Initiative");

            var options = new ApiOptions()
            {
                PageSize = 100,
                PageCount = 100
            };

            var closedIssues = await _client.Issue.GetAllForRepository("NuGet", repo, request, options);

            List<Issue> issues = new();
            foreach (var issue in closedIssues)
            {
                if (issue.ClosedAt == null) { continue; }

                var closedAt = new DateOnly(issue.ClosedAt.Value.Year, issue.ClosedAt.Value.Month, issue.ClosedAt.Value.Day);
                if (closedAt >= sprintStart && closedAt <= sprintEnd)
                {
                    issues.Add(issue);
                }
            }

            return issues;
        }

        private Action<IReadOnlyList<Issue>> GetOutputter(OutputFormat outputFormat)
        {
            switch (outputFormat)
            {
                case OutputFormat.Console: return ConsoleOutput.Write;
                case OutputFormat.Md: return MarkdownOutput.Write;
                default: throw new NotSupportedException("Unknown OutputFormat " + outputFormat);
            }
        }

        public enum OutputFormat { Console, Md };

        private class CiInitiativeCommandFactory : ICommandFactory
        {
            public Command CreateCommand(Type type, GitHubPatBinder patBinder)
            {
                var command = new Command("CiInitiative");
                command.Description = "Find completed CI Initiative work.";

                var sprint = new Option<string>("--sprint");
                sprint.AddAlias("-s");
                sprint.Description = "Sprint name";
                sprint.IsRequired = true;
                command.AddOption(sprint);

                var output = new Option<OutputFormat>("--output");
                output.AddAlias("-o");
                output.Description = "Output format";
                output.SetDefaultValue(OutputFormat.Console);
                command.AddOption(output);

                command.SetHandler<GitHubPat, string, OutputFormat>(RunAsync,
                    patBinder, sprint, output);

                return command;
            }

            public async Task RunAsync(GitHubPat pat, string sprint, OutputFormat outputFormat)
            {
                var serviceProvider = new ServiceCollection()
                    .AddGithubIssueTagger(pat)
                    .BuildServiceProvider();

                var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
                using (scopeFactory.CreateScope())
                {
                    var report = serviceProvider.GetRequiredService<CiInitiativeReport>();
                    await report.RunAsync(sprint, outputFormat);
                }
            }
        }
    }
}
