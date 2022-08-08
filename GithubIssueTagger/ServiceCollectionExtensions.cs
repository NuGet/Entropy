using GithubIssueTagger.GraphQL;
using GithubIssueTagger.Reports;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using System;

namespace GithubIssueTagger
{
    internal static class ServiceCollectionExtensions
    {
        private const string UserAgent = "nuget-github-issue-tagger";

        public static ServiceCollection AddGithubIssueTagger(this ServiceCollection serviceCollection, GitHubPat pat)
        {
            serviceCollection.AddSingleton(pat);
            serviceCollection.AddSingleton<QueryCache>();
            serviceCollection.AddSingleton(GitHubClientFactory);
            serviceCollection.AddSingleton(GitHubGraphQLClientFactory);

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

        private static GitHubClient GitHubClientFactory(IServiceProvider services)
        {
            var client = new GitHubClient(new ProductHeaderValue(UserAgent));

            var pat = services.GetRequiredService<GitHubPat>();
            if (pat?.Value == null)
            {
                Console.WriteLine("Warning: Unable to get github token. Making unauthenticated HTTP requests, which has lower request limits and cannot access private repos.");
            }
            else
            {
                client.Credentials = new Credentials(pat.Value);
            }

            return client;
        }

        private static GitHubGraphQLClient GitHubGraphQLClientFactory(IServiceProvider services)
        {
            var pat = services.GetRequiredService<GitHubPat>();
            return new GitHubGraphQLClient(pat, UserAgent);
        }
    }
}
