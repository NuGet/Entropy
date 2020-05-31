using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using PackageHelper.Csv;

namespace PackageHelper.Commands
{
    class MergeTestResults
    {
        public static Command GetCommand()
        {
            var command = new Command("merge-test-results")
            {
                Description = "Read test result files and merge them to a single CSV",
            };

            command.Add(new Option<string>("--input-dir")
            {
                Description = "The directory to scan for recognized test result files",
            });
            command.Add(new Option<string>("--output-path")
            {
                Description = "The file path to write the test results to (should end in .csv)",
            });

            command.Handler = CommandHandler.Create<string, string>(Execute);

            return command;
        }

        private static int Execute(string inputDir, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(inputDir))
            {
                if (!Helper.TryFindRoot(out var rootDir))
                {
                    return 1;
                }

                var outDir = Path.Combine(rootDir, "out");

                if (string.IsNullOrWhiteSpace(inputDir))
                {
                    inputDir = outDir;
                }
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.Combine(inputDir, "merged-test-results.csv");
            }

            Console.WriteLine($"Input directory: {inputDir}");
            Console.WriteLine($"Output path:     {outputPath}");

            var dir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var writer = new BackgroundCsvWriter<TestResultRecord>(outputPath, gzip: false))
            {
                var testResultIndex = 0;

                foreach (var restoreResult in CsvUtility.EnumerateRestoreResults(inputDir))
                {
                    if (restoreResult.ScenarioName != "warmup" && restoreResult.ScenarioName != "arctic")
                    {
                        Console.WriteLine($"Skipping restore scenario '{restoreResult.ScenarioName}'.");
                        continue;
                    }

                    writer.Add(new TestResultRecord
                    {
                        TimestampUtc = Helper.GetExcelTimestamp(DateTimeOffset.Parse(restoreResult.TimestampUtc)),
                        VariantName = restoreResult.VariantName,
                        SolutionName = restoreResult.SolutionName,
                        TestType = TestType.Restore,
                        MachineName = restoreResult.MachineName,
                        TestResultIndex = testResultIndex,
                        IsWarmUp = restoreResult.IsWarmUp(),
                        Iteration = restoreResult.Iteration,
                        Iterations = restoreResult.IterationCount,
                        DurationMs = restoreResult.TotalTimeSeconds * 1000,
                        LogFileName = restoreResult.LogFileName,
                        Dependencies = true,
                    });

                    testResultIndex++;
                }

                foreach (var replayResult in CsvUtility.EnumerateReplayResults(inputDir))
                {
                    writer.Add(new TestResultRecord
                    {
                        TimestampUtc = replayResult.TimestampUtc,
                        VariantName = replayResult.VariantName,
                        SolutionName = replayResult.SolutionName,
                        TestType = TestType.Replay,
                        MachineName = replayResult.MachineName,
                        TestResultIndex = testResultIndex,
                        IsWarmUp = replayResult.IsWarmUp,
                        Iteration = replayResult.Iteration,
                        Iterations = replayResult.Iterations,
                        DurationMs = replayResult.DurationMs,
                        LogFileName = replayResult.LogFileName,
                        Dependencies = replayResult.Dependencies,
                    });

                    testResultIndex++;
                }

                Console.WriteLine($"Wrote {testResultIndex} test results.");
            }

            return 0;
        }
    }
}
