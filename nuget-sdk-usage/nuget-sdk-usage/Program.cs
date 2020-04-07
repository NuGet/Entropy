using nuget_sdk_usage.Analysis.Scanning;
using nuget_sdk_usage.Updater;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace nuget_sdk_usage
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // https://github.com/dotnet/command-line-api/issues/817
            if (args.Length == 0)
            {
                Console.WriteLine("Pass either the 'scan' or 'update' commands.");
                return;
            }

            var rootCommand = new RootCommand()
            {
                Scanner.GetCommand(),
                Update.GetCommand()
            };

            var cm = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .Build();

            await cm.InvokeAsync(args);
        }

    }
}
