using Microsoft.Extensions.DependencyInjection;
using Octokit;
using System;
using System.CommandLine;

namespace GithubIssueTagger.Reports
{
    internal class SimpleCommandFactory : ICommandFactory
    {
        public Command CreateCommand(Type type, GitHubPatBinder patBinder)
        {
            var command = new Command(type.Name);

            command.SetHandler(async (GitHubPat pat) =>
            {
                var serviceProvider = new ServiceCollection()
                  .AddGithubIssueTagger(pat)
                  .BuildServiceProvider();

                var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
                using (scopeFactory.CreateScope())
                {
                    var report = (IReport)serviceProvider.GetRequiredService(type);
                    await report.RunAsync();
                }
            },
            patBinder);

            return command;
        }
    }
}
