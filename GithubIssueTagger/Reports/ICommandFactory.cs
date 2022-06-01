using System;
using System.CommandLine;

namespace GithubIssueTagger.Reports
{
    internal interface ICommandFactory
    {
        /// <summary>
        /// Interface for factories that create Command objects for report types.
        /// </summary>
        /// <param name="type">A type that must implement IReport.</param>
        /// <returns>A Command that will be added to the System.CommandLine.RootCommand.</returns>
        Command CreateCommand(Type type, GitHubClientBinder clientBinder);
    }
}
