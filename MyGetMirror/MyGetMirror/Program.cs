using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace MyGetMirror
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var application = new CommandLineApplication();
            application.Name = AppDomain.CurrentDomain.FriendlyName;

            var sourceOption = application.Option(
                "--source",
                "The NuGet source to read packages from.",
                CommandOptionType.SingleValue);

            var destinationOption = application.Option(
                "--destination",
                "The NuGet source to publish packages to. This should be the actual source URL, not the publish URL.",
                CommandOptionType.SingleValue);

            var destinationApiKeyOption = application.Option(
                "--destinationApiKey",
                "The API key to use when pushing to the destination.",
                CommandOptionType.SingleValue);

            var excludeNuGetSymbolsOption = application.Option(
                "--excludeNuGetSymbols",
                "Specify this option to ignore symbols package on the source and to not push them to the destination.",
                CommandOptionType.NoValue);

            var overwriteExistingOption = application.Option(
                "--overwriteExisting",
                "Specify thie option to overwrite existing packages on the destination by pushing all packages from the source.",
                CommandOptionType.NoValue);

            var maxDegreeOfParallelismOption = application.Option(
                "--maxDegreeOfParallelism",
                "The maximum degree of parallelism to use when pushing packages to the destination.",
                CommandOptionType.SingleValue);

            application.OnExecute(() =>
            {
                var validInput = true;

                if (!sourceOption.HasValue())
                {
                    Console.WriteLine($"The --{sourceOption.LongName} option is required.");
                    validInput = false;
                }
                
                if (!destinationOption.HasValue())
                {
                    Console.WriteLine($"The --{destinationOption.LongName} option is required.");
                    validInput = false;
                }

                if (!destinationApiKeyOption.HasValue())
                {
                    Console.WriteLine($"The --{destinationApiKeyOption.LongName} option is required.");
                    validInput = false;
                }

                int maxDegreeOfParallelism = 16;
                if (maxDegreeOfParallelismOption.HasValue() &&
                    (!int.TryParse(maxDegreeOfParallelismOption.Value(), out maxDegreeOfParallelism) || maxDegreeOfParallelism < 1))
                {
                    Console.WriteLine($"The --{maxDegreeOfParallelismOption.LongName} option must have a positive integer value.");
                    validInput = false;
                }

                if (!validInput)
                {
                    application.ShowHelp();
                    return 1;
                }

                var request = new FeedMirrorRequest
                {
                    Source = sourceOption.Value(),
                    Destination = destinationOption.Value(),
                    DestinationApiKey = destinationApiKeyOption.Value(),
                    IncludeNuGet = true,
                    IncludeVsix = true,
                    IncludeNuGetSymbols = !excludeNuGetSymbolsOption.HasValue(),
                    OverwriteExisting = overwriteExistingOption.HasValue(),
                    MaxDegreeOfParallelism = maxDegreeOfParallelism
                };
                
                ExecuteAsync(request, CancellationToken.None).Wait();

                return 0;
            });

            return application.Execute(args);
        }

        private static async Task ExecuteAsync(FeedMirrorRequest request, CancellationToken token)
        {
            request.PackagesDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(request.PackagesDirectory);

            try
            {
                var logger = new ConsoleLogger();

                var command = new FeedMirrorCommand();
                await command.ExecuteAsync(request, logger, token);
            }
            finally
            {
                Directory.Delete(request.PackagesDirectory, recursive: true);
            }
        }
    }
}
