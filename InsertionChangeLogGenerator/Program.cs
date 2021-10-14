using CommandLine;
using Octokit;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InsertionChangeLogGenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<GenerateOptions>(args)
               .MapResult(
                 (GenerateOptions copyOptions) => RunGenerateCommand(copyOptions),
                 errs => 1);
        }

        private static int RunGenerateCommand(GenerateOptions opts)
        {
            return RunGenerateCommandAsync(opts).GetAwaiter().GetResult();
        }

        private static async Task<int> RunGenerateCommandAsync(GenerateOptions opts)
        {
            var githubClient = new GitHubClient(new ProductHeaderValue("nuget-github-insertion-changelog-tagger"))
            {
                Credentials = new Credentials(opts.PAT)
            };
            var startSha = opts.StartSha;
            var branch = opts.Branch;
            var directory = opts.Output ?? Directory.GetCurrentDirectory();

            Console.WriteLine($"Generating change log for:" + Environment.NewLine +
                $"Sha: {startSha}" + Environment.NewLine +
                $"Branch: {branch}" + Environment.NewLine +
                $"Output path: {directory}"
                );
            try
            {
                await ChangeLogGenerator.GenerateInsertionChangelogForNuGetClient(githubClient, startSha, branch, directory);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed with {e}");
                return -1;
            }

            return 0;
        }

        class BaseOptions
        {
            [Option("pat", Required = true, HelpText = "A Github PAT from a user with sufficient permissions to perform the invoked action.")]
            public string PAT { get; set; }
        }

        [Verb("generate", HelpText = "Generate an insertion changelog.")]
        class GenerateOptions : BaseOptions
        {
            [Option("startSha", Required = true, HelpText = "The sha from which to start the generator.")]
            public string StartSha { get; set; }

            [Option("branch", Required = true, HelpText = "The branch in which to search for the start sha.")]
            public string Branch { get; set; }

            [Option("output", Required = false, HelpText = "Directory to output the results file in.")]
            public string Output { get; set; }
        }
    }
}
