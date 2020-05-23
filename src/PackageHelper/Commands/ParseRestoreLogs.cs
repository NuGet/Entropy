using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using PackageHelper.RestoreReplay;

namespace PackageHelper.Commands
{
    static class ParseRestoreLogs
    {
        public static Command GetCommand()
        {
            var command = new Command("parse-restore-logs")
            {
                Description = "Parse restore logs into serialized request graphs",
            };

            command.Add(new Option<int>(
                "--max-logs-per-graph",
                getDefaultValue: () => int.MaxValue)
            {
                Description = "Max number of restore logs that will be merged into a single request graph"
            });

            command.Handler = CommandHandler.Create<int>(Execute);

            return command;
        }

        static int Execute(int maxLogsPerGraph)
        {
            if (!Helper.TryFindRoot(out var rootDir))
            {
                return 1;
            }

            if (maxLogsPerGraph == int.MaxValue)
            {
                Console.WriteLine($"The first {maxLogsPerGraph} restore logs will be used per request graph.");
            }
            else
            {
                Console.WriteLine($"No limit will be applied to the number of restore logs per request graph.");
            }

            var logDir = Path.Combine(rootDir, "out", "logs");
            var graphs = LogParser.ParseAndMergeRestoreRequestGraphs(logDir, maxLogsPerGraph);
            var writtenNames = new HashSet<string>();
            for (int index = 0; index < graphs.Count; index++)
            {
                var graph = graphs[index];

                string fileName;
                if (graph.VariantName != null)
                {
                    fileName = $"requestGraph-{graph.VariantName}-{graph.SolutionName}";
                }
                else
                {
                    fileName = $"requestGraph-{graph.SolutionName}";
                }

                if (writtenNames.Contains(fileName))
                {
                    Console.WriteLine($" WARNING: The output file {fileName} has already been written.");
                    Console.WriteLine($" Consider including a variant name in the restore log file name to differentiate variants.");
                    Console.WriteLine($" Output data is grouped by variant name, solution name, and set of package sources.");
                }

                Console.WriteLine($"Preparing {fileName}...");
                GraphOperations.LazyTransitiveReduction(graph.Graph);

                var filePath = Path.Combine(rootDir, "out", "request-graphs", fileName);
                var outDir = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(outDir);
                Console.WriteLine($"  Writing {filePath}...");
                RequestGraphSerializer.WriteToGraphvizFile(Path.Combine(rootDir, "out", "request-graphs", $"{fileName}.gv"), graph.Graph);
                RequestGraphSerializer.WriteToFile($"{filePath}.json.gz", graph.Graph);
                writtenNames.Add(fileName);
            }

            return 0;
        }
    }
}
