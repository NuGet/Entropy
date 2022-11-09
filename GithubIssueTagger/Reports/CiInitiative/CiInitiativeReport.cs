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
        public Task RunAsync()
        {
            Console.WriteLine("This report cannot run in interactive mode");
            return Task.CompletedTask;
        }

        internal static async Task RunAsync(GitHubClient client, Args args)
        {
            IReadOnlyList<Issue> issues = await GetCompletedCiIssuesBetweenDatesAsync(client, args.Start, args.End);
            var orderedIssues = OrderIssues(issues, args.Order);

            var outputter = GetOutputter(args.OutputFormat);
            outputter(orderedIssues);
        }

        private static async Task<IReadOnlyList<Issue>> GetCompletedCiIssuesBetweenDatesAsync(GitHubClient client, DateOnly start, DateOnly end)
        {
            var tasks = new Task<IReadOnlyList<Issue>>[]
            {
                GetClosedCiIssuesAsync(client, "Home", start, end),
                GetClosedCiIssuesAsync(client, "Client.Engineering", start, end)
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

        private static async Task<IReadOnlyList<Issue>> GetClosedCiIssuesAsync(GitHubClient client, string repo, DateOnly sprintStart, DateOnly sprintEnd)
        {
            var request = new RepositoryIssueRequest()
            {
                Since = sprintStart.ToDateTime(TimeOnly.MinValue),
                State = ItemStateFilter.Closed
            };
            request.Labels.Add("Engineering Productivity");

            var options = new ApiOptions()
            {
                PageSize = 100,
                PageCount = 100
            };

            var closedIssues = await client.Issue.GetAllForRepository("NuGet", repo, request, options);

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

        private static IEnumerable<Issue> OrderIssues(IReadOnlyList<Issue> issues, Order order)
        {
            switch (order)
            {
                case Order.Date:
                    return issues.OrderBy(i => i.ClosedAt);

                case Order.Assignee:
                    return issues.OrderBy(i => i.Assignee?.Login ?? string.Empty)
                        .ThenBy(i => i.ClosedAt);

                default:
                    throw new Exception("Unknown order " + order);
            }
        }

        private static Action<IEnumerable<Issue>> GetOutputter(OutputFormat outputFormat)
        {
            switch (outputFormat)
            {
                case OutputFormat.Console: return ConsoleOutput.Write;
                case OutputFormat.Md: return MarkdownOutput.Write;
                default: throw new NotSupportedException("Unknown OutputFormat " + outputFormat);
            }
        }

        public enum OutputFormat { Console, Md }

        public enum Order { Date, Assignee }

        internal class Args
        {
            public required DateOnly Start { get; init; }
            public required DateOnly End { get; init; }
            public required OutputFormat OutputFormat { get; init; }
            public required Order Order { get; init; }
        }

        private class CiInitiativeCommandFactory : ICommandFactory
        {
            public Command CreateCommand(Type type, GitHubPatBinder patBinder)
            {
                var command = new Command(nameof(CiInitiative));
                command.Description = "Find completed CI Initiative work.";

                var format = new Option<OutputFormat>("--format");
                format.AddAlias("-f");
                format.Description = "Output format";
                format.SetDefaultValue(OutputFormat.Console);

                var order = new Option<Order>("--order");
                order.AddAlias("-o");
                order.Description = "Output order";
                order.SetDefaultValue(Order.Date);

                var sprint = SprintReport.GetCommand(patBinder, format, order);
                command.Add(sprint);
                var between = BetweenReport.GetCommand(patBinder, format, order);
                command.Add(between);

                return command;
            }
        }
    }
}
