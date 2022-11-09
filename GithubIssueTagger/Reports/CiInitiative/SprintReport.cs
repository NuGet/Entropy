using Microsoft.Extensions.DependencyInjection;
using Octokit;
using System.CommandLine;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports.CiInitiative
{
    internal class SprintReport
    {
        internal static Command GetCommand(GitHubPatBinder gitHubPatBinder, Option<CiInitiativeReport.OutputFormat> format, Option<CiInitiativeReport.Order> order)
        {
            var command = new Command("sprint");
            command.Description = "Get CI Initiative issues from a sprint";

            var sprint = new Argument<string>();
            sprint.Description = "The sprint name";
            sprint.Arity = ArgumentArity.ExactlyOne;
            command.Add(sprint);

            command.Add(format);
            command.Add(order);

            command.SetHandler<GitHubPat, string, CiInitiativeReport.OutputFormat, CiInitiativeReport.Order>(RunAsync,
                gitHubPatBinder, sprint, format, order);

            return command;
        }

        private static async Task RunAsync(GitHubPat pat, string sprint, CiInitiativeReport.OutputFormat format, CiInitiativeReport.Order order)
        {
            var serviceProvider = new ServiceCollection()
                .AddGithubIssueTagger(pat)
                .BuildServiceProvider();

            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            using (scopeFactory.CreateScope())
            {
                var client = serviceProvider.GetRequiredService<GitHubClient>();

                var (start, end) = SprintUtilities.GetSprintStartAndEnd(sprint);
                var args = new CiInitiativeReport.Args
                {
                    Start = start,
                    End = end,
                    OutputFormat = format,
                    Order = order,
                };
                await CiInitiativeReport.RunAsync(client, args);
            }
        }
    }
}
