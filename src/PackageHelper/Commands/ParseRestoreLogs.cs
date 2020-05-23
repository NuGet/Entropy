using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PackageHelper.RestoreReplay;

namespace PackageHelper.Commands
{
    static class ParseRestoreLogs
    {
        public const string Name = "parse-restore-logs";
        private const int DefaultMaxLogsPerGraph = int.MaxValue;

        public static Task<int> ExecuteAsync(string[] args)
        {
            if (!Helper.TryFindRoot(out var rootDir))
            {
                return Task.FromResult(1);
            }

            var maxLogsPerGraph = DefaultMaxLogsPerGraph;
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], out maxLogsPerGraph))
                {
                    maxLogsPerGraph = DefaultMaxLogsPerGraph;
                    Console.WriteLine($"The second argument for the {Name} command was ignored because it's not an integer.");
                }
                else
                {
                    Console.WriteLine($"The max logs-per-graph argument of {maxLogsPerGraph} will be used.");
                }
            }
            else
            {
                Console.WriteLine($"No max logs-per-graph restriction will be applied.");
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

            return Task.FromResult(0);
        }
    }
}
