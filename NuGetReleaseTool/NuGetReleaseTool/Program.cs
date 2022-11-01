// See https://aka.ms/new-console-template for more information
using CommandLine;
using ReleaseNotesGenerator;

Console.WriteLine("Hello, World!");

ParserResult<Options> parserResult = Parser.Default.ParseArguments<Options>(args);
parserResult
    .WithParsed(options =>
    {
        var task = Task.Run(async () =>
        {
            try
            {
                var fileName = "NuGet-" + options.Release + ".md";

                File.WriteAllText(fileName, await new ReleaseNotesGenerator.ReleaseNotesGenerator(options).GenerateChangelog());
                Console.WriteLine($"{fileName} creation complete");
                Environment.Exit(0);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        });
        task.Wait();
    })
    .WithNotParsed(errors =>
    {
        Environment.Exit(1);
        Console.ReadLine();
    });
