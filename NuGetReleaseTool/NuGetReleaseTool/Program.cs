using CommandLine;
using NuGetReleaseTool;
using NuGetReleaseTool.AddMilestoneCommand;
using NuGetReleaseTool.GenerateInsertionChangelogCommand;
using NuGetReleaseTool.GenerateRedundantPackageListCommand;
using NuGetReleaseTool.GenerateReleaseChangelogCommand;
using NuGetReleaseTool.GenerateReleaseNotesCommand;
using NuGetReleaseTool.ValidateReleaseCommand;
using Octokit;

return Parser.Default.ParseArguments<GenerateReleaseNotesCommandOptions, ValidateReleaseCommandOptions, GenerateInsertionChangelogCommandOptions, GenerateReleaseChangelogCommandOptions, UnlistRedundantPackagesCommandOptions, AddMilestoneCommandOptions>(args)
               .MapResult(
                 (GenerateReleaseNotesCommandOptions generateOpts) => RunReleaseNotesGeneratorCommand(generateOpts),
                 (ValidateReleaseCommandOptions validateOpts) => RunReleaseValidateCommand(validateOpts),
                (GenerateInsertionChangelogCommandOptions insertionOpts) => RunGenerateInsertionChangelog(insertionOpts),
                (GenerateReleaseChangelogCommandOptions releaseChangelogOpts) => RunGenerateReleaseChangelogCommand(releaseChangelogOpts),
                (UnlistRedundantPackagesCommandOptions unlistOpts) => RunGenerateUnlistRedundantPackagesCommand(unlistOpts),
                (AddMilestoneCommandOptions addMilestoneOpts) => RunAddMilestoneCommand(addMilestoneOpts),
                 errs => 1);

static int RunReleaseNotesGeneratorCommand(GenerateReleaseNotesCommandOptions opts)
{
    return RunAsync(opts).GetAwaiter().GetResult();
    async Task<int> RunAsync(GenerateReleaseNotesCommandOptions options)
    {
        var fileName = "NuGet-" + options.Release + ".md";
        var githubClient = GenerateGitHubClient(opts);
        File.WriteAllText(fileName, await new ReleaseNotesGenerator(options, githubClient).GenerateChangelog());
        Console.WriteLine($"{fileName} creation complete");
        return 0;
    }
}

static int RunReleaseValidateCommand(ValidateReleaseCommandOptions opts)
{
    return RunAsync(opts).GetAwaiter().GetResult();
    async Task<int> RunAsync(ValidateReleaseCommandOptions options)
    {
        var githubClient = GenerateGitHubClient(options);
        var validateReleaseCommand = new ValidateReleaseCommand(options, githubClient);
        return await validateReleaseCommand.RunAsync();
    }
}

static int RunGenerateInsertionChangelog(GenerateInsertionChangelogCommandOptions opts)
{
    return RunAsync(opts).GetAwaiter().GetResult();
    async Task<int> RunAsync(GenerateInsertionChangelogCommandOptions opts)
    {
        GitHubClient githubClient = GenerateGitHubClient(opts);

        var startSha = opts.StartCommit;
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

static int RunAddMilestoneCommand(AddMilestoneCommandOptions opts)
{
    return RunAsync(opts).GetAwaiter().GetResult();
    async Task<int> RunAsync(AddMilestoneCommandOptions options)
    {
        var githubClient = GenerateGitHubClient(options);
        var command = new AddMilestoneCommand(options, githubClient);
        await command.RunAsync();
        return 0;
    }
}

static int RunGenerateReleaseChangelogCommand(GenerateReleaseChangelogCommandOptions opts)
{
    return RunAsync(opts).GetAwaiter().GetResult();
    async Task<int> RunAsync(GenerateReleaseChangelogCommandOptions options)
    {
        var githubClient = GenerateGitHubClient(options);
        var releaseChangelogGenerator = new ReleaseChangelogGenerator(options, githubClient);
        await releaseChangelogGenerator.RunAsync();
        return 0;
    }
}

static int RunGenerateUnlistRedundantPackagesCommand(UnlistRedundantPackagesCommandOptions opts)
{
    return RunAsync(opts).GetAwaiter().GetResult();
    async Task<int> RunAsync(UnlistRedundantPackagesCommandOptions options)
    {
        var releaseChangelogGenerator = new UnlistRedudantPackagesCommand(options);
        await releaseChangelogGenerator.RunAsync();
        return 0;
    }
}

static GitHubClient GenerateGitHubClient(BaseOptions opts)
{
    string NuGet = "nuget";
    string Home = "home";
    var githubClient = new GitHubClient(new ProductHeaderValue("nuget-release-tool"));

    if (!string.IsNullOrEmpty(opts.GitHubToken))
    {
        githubClient.Credentials = new Credentials(opts.GitHubToken);
    }
    else
    {
        Dictionary<string, string>? credentuals = GitCredentials.Get(new Uri($"https://github.com/{NuGet}/{Home}"));
        if (credentuals?.TryGetValue("password", out string? pat) == true)
        {
            githubClient.Credentials = new Credentials(pat);
        }
        else
        {
            Console.WriteLine("Warning: Unable to get github token. Making unauthenticated HTTP requests, which has lower request limits.");
        }
    }
    return githubClient;
}