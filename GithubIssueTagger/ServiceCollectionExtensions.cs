using GithubIssueTagger.Reports;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using System;

namespace GithubIssueTagger
{
    internal static class ServiceCollectionExtensions
    {
        public static ServiceCollection AddGithubIssueTagger(this ServiceCollection serviceCollection, GitHubClient client)
        {
            serviceCollection.AddSingleton(client);
            serviceCollection.AddSingleton<QueryCache>();

            Type reportType = typeof(IReport);
            foreach (Type type in reportType.Assembly.GetTypes())
            {
                if (type.IsClass && type.IsAssignableTo(reportType))
                {
                    serviceCollection.AddSingleton(type);
                }
            }

            return serviceCollection;
        }
    }
}
