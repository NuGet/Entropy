using System;
using System.Collections.Generic;
using GithubIssueTagger.GraphQL;

namespace GithubIssueTagger.Reports.IceBox
{
    /// <summary>
    /// Writes GitHub Actions workflow commands so that messages surface as annotations on the
    /// workflow run. See https://docs.github.com/actions/using-workflows/workflow-commands-for-github-actions.
    /// </summary>
    internal static class GitHubActionsLog
    {
        public static void Warning(string message)
        {
            Console.WriteLine("::warning ::" + message);
        }

        public static void Error(string message)
        {
            Console.WriteLine("::error ::" + message);
        }

        public static void GraphQlErrors(IReadOnlyList<GraphQLResponseError> errors)
        {
            foreach (GraphQLResponseError error in errors)
            {
                if (error.Message != null)
                {
                    Error(error.Message);
                }
            }
        }
    }
}
