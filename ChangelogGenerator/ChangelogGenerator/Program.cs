using CommandLine;
using System;
using System.IO;
using System.Threading.Tasks;


namespace ChangelogGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            ParserResult<Options> parserResult = Parser.Default.ParseArguments<Options>(args);
            parserResult
                .WithParsed(options =>
                {
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            var fileName = "NuGet-" + options.Release
                                + (string.IsNullOrEmpty(options.RequiredLabel) ? "" : options.RequiredLabel) + ".md";

                            File.WriteAllText(fileName, await new ChangelogGenerator(options).GenerateChangelog());
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
        }
    }
}
