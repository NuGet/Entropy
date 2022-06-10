using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GithubIssueTagger.Reports.CiInitiative
{
    internal static class ConsoleOutput
    {
        public static void Write(IReadOnlyList<Issue> issues)
        {
            foreach (var issue in issues)
            {
                var assignees = string.Join(",", issue.Assignees.Select(a => a.Login));
                Console.WriteLine("{0} {1}", assignees, issue.HtmlUrl);
            }
        }
    }
}
