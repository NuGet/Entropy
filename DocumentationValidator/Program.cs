using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Octokit;

namespace DocumentationValidator
{
    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<GetLogCodesOptions, GenerateIssuesOptions>(args)
               .MapResult(
                 (GetLogCodesOptions getLogCodesOptions) => RunGetLogCodesCommand(getLogCodesOptions),
                 (GenerateIssuesOptions generateIssuesOptions) => GenerateIssuesCommand(generateIssuesOptions),
                 errs => 1);
        }

        private static int RunGetLogCodesCommand(GetLogCodesOptions opts)
        {
            return RunGetLogCodesCommandAsync(opts).GetAwaiter().GetResult();
            async Task<int> RunGetLogCodesCommandAsync(GetLogCodesOptions opts)
            {
                NuGetLogCodeAnalyzer analyzer = new(pat: null, Console.Out);
                var codes = await analyzer.GetUndocumentedLogCodesAsync();

                if (codes.Any())
                {
                    PrintUtilities.PrintIssuesInMarkdownTableAsync(codes, Console.Error);
                    return 1;
                }
                return 0;
            }
        }

        private static int GenerateIssuesCommand(GenerateIssuesOptions opts)
        {
            return GenerateIssuesCommandAsync(opts).GetAwaiter().GetResult();
            async Task<int> GenerateIssuesCommandAsync(GenerateIssuesOptions opts)
            {
                Dictionary<string, string> credentuals = GitCredentials.Get(new Uri("https://github.com/nuget/docs.microsoft.com-nuget"));
                string pat = null;
                if (!credentuals?.TryGetValue("password", out pat) == true) {
                    Console.WriteLine("Warning: Unable to get github token. Making unauthenticated HTTP requests, which has lower request limits.");
                }

                NuGetLogCodeAnalyzer analyzer = new(pat, Console.Out);
                var codes = await analyzer.GetUndocumentedLogCodesAsync();

                if (codes.Any())
                {
                    await analyzer.CreateIssuesAsync(codes);
                }
                PrintUtilities.PrintIssuesInMarkdownTableAsync(codes, Console.Error);
                return 0;
            }
        }

        [Verb("get-undocumented-codes", HelpText = "Generates a markdown table of the undocumented log codes with their relevant issues if any.")]
        class GetLogCodesOptions
        {
        }

        [Verb("generate-issues", HelpText = "Creates issues in the docs repo for the log codes that are undocumented.")]
        class GenerateIssuesOptions
        {
        }
    }
}
