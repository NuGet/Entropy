using CommandLine;
using GenerateReleaseNotesCommand;
using InsertionChangeLogGenerator;
using NuGetReleaseTool;
using NuGetReleaseTool.GenerateInsertionChangelogCommand;
using NuGetReleaseTool.ValidateReleaseCommand;
using Octokit;

return Parser.Default.ParseArguments<GenerateReleaseNotesCommandOptions, ValidateReleaseCommandOptions, GenerateInsertionChangelogCommandOptions>(args)
               .MapResult(
                 (GenerateReleaseNotesCommandOptions generateOpts) => RunReleaseNotesGeneratorCommand(generateOpts),
                 (ValidateReleaseCommandOptions validateOpts) => RunReleaseValidateCommand(validateOpts),
                (GenerateInsertionChangelogCommandOptions insertionOpts) => RunGenerateInsertionChangelog(insertionOpts),
                 errs => 1);

static int RunReleaseNotesGeneratorCommand(GenerateReleaseNotesCommandOptions opts)
{
    return RunReleaseNotesGeneratorCommandAsync(opts).GetAwaiter().GetResult();
    async Task<int> RunReleaseNotesGeneratorCommandAsync(GenerateReleaseNotesCommandOptions options)
    {
        var fileName = "NuGet-" + options.Release + ".md";
        File.WriteAllText(fileName, await new ReleaseNotesGenerator(options).GenerateChangelog());
        Console.WriteLine($"{fileName} creation complete");
        return 0;
    }
}

static int RunReleaseValidateCommand(ValidateReleaseCommandOptions opts)
{
    return RunReleaseValidateCommandAsync(opts).GetAwaiter().GetResult();
    async Task<int> RunReleaseValidateCommandAsync(ValidateReleaseCommandOptions options)
    {
        // Check release notes.
        // Check nuget.exe.
        // Check docs issues. Check undocumented log codes.

        Console.WriteLine("You have succesfully run the release validate command");
        return 0;
    }
}

static int RunGenerateInsertionChangelog(GenerateInsertionChangelogCommandOptions opts)
{
    return RunGenerateCommandAsync(opts).GetAwaiter().GetResult();
    async Task<int> RunGenerateCommandAsync(GenerateInsertionChangelogCommandOptions opts)
    {
        var githubClient = new GitHubClient(new ProductHeaderValue("nuget-github-insertion-changelog-tagger"));

        if (!string.IsNullOrEmpty(opts.GitHubToken))
        {
            githubClient.Credentials = new Credentials(opts.GitHubToken);
        }
        else
        {
            Dictionary<string, string> credentuals = GitCredentials.Get(new Uri("https://github.com/NuGet/Home"));
            if (credentuals?.TryGetValue("password", out string pat) == true)
            {
                githubClient.Credentials = new Credentials(pat);
            }
            else
            {
                Console.WriteLine("Warning: Unable to get github token. Making unauthenticated HTTP requests, which has lower request limits.");
            }
        }

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

}

