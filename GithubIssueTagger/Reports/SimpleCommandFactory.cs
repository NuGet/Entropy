using Microsoft.Extensions.DependencyInjection;
using Octokit;
using System;
using System.CommandLine;

namespace GithubIssueTagger.Reports
{
    internal class SimpleCommandFactory : ICommandFactory
    {
        public Command CreateCommand(Type type, GitHubClientBinder clientBinder)
        {
            var command = new Command(type.Name);

            command.SetHandler(async (GitHubClient client) =>
            {
                var serviceProvider = new ServiceCollection()
                  .AddGithubIssueTagger(client)
                  .BuildServiceProvider();

                var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
                using (scopeFactory.CreateScope())
                {
                    var report = (IReport)serviceProvider.GetRequiredService(type);
                    await report.RunAsync();
                }
            });

            return command;
        }
    }
}
