using NuGet.Common;
using System.Collections.Generic;
using System.IO;

namespace DocumentationValidator
{
    internal static class PrintUtilities
    {
        internal static void PrintIssuesInMarkdownTableAsync(Dictionary<NuGetLogCode, List<string>> issuesForLogCodes, TextWriter logger)
        {
            logger.WriteLine("| Log Code | Potential Issues |");
            logger.WriteLine("|----------|------------------|");

            foreach (var logCode in issuesForLogCodes)
            {
                logger.WriteLine($"| {logCode.Key} | {string.Join(" ,", logCode.Value)} |");
            }
        }
    }
}
