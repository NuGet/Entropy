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

        public static Task<int> ExecuteAsync(string[] args)
        {
            if (!Helper.TryFindRoot(out var rootDir))
            {
                return Task.FromResult(1);
            }

            var logDir = Path.Combine(rootDir, "out", "logs");
            var graphs = LogParser.ParseAndMergeRestoreRequestGraphs(logDir);
            var writtenNames = new HashSet<string>();
            for (int index = 0; index < graphs.Count; index++)
            {
                var graph = graphs[index];

                string fileName;
                if (graph.VariantName != null)
                {
                    fileName = $"requestGraph-{graph.VariantName}-{graph.SolutionName}.json.gz";
                }
                else
                {
                    fileName = $"requestGraph-{graph.SolutionName}.json.gz";
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
                RequestGraphSerializer.WriteToFile(filePath, graph.Graph);
                writtenNames.Add(fileName);
            }

            return Task.FromResult(0);
        }
    }
}
