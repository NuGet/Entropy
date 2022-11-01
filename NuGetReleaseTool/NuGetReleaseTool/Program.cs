using CommandLine;
using GenerateReleaseNotesCommand;

return Parser.Default.ParseArguments<GenerateReleaseNotesCommandOptions>(args)
               .MapResult(
                 (GenerateReleaseNotesCommandOptions releaseNotesGeneratorOptions) => RunReleaseNotesGeneratorCommand(releaseNotesGeneratorOptions),
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
