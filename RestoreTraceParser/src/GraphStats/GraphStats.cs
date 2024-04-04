using System;
using System.IO;
using Microsoft.Diagnostics.Tracing;

namespace RestoreTraceParser
{
    public static class GraphStats
    {
        // Can be called from Main to generate statistics about the NuGet graph.
        // Depends on event "Microsoft-NuGet/WalkAsyncTransitivePackage", which is a prototype
        // event and not present in mainline builds.  The event is of the form:
        //      Package (string)
        //      TransitiveHitCount (int)
        public static void Execute(string[] args)
        {
            PackageTable packageTable = new PackageTable();

            string sourceDirectory = args[1];
            string[] traceFiles = System.IO.Directory.GetFiles(sourceDirectory, "*.etl");
            bool firstTrace = true;
            foreach (string traceFile in traceFiles)
            {
                if (traceFile.EndsWith(".clrRundown.etl") || traceFile.EndsWith(".kernel.etl"))
                {
                    continue;
                }

                if (firstTrace)
                {
                    firstTrace = false;
                }
                else
                {
                    packageTable.IncrementRunIndex();
                }
                Console.WriteLine($"Processing {traceFile}");
                ProcessTrace(traceFile, packageTable);
            }

            if (packageTable.AllRunsIdentical())
            {
                Console.WriteLine("All runs are identical.");
            }
            else
            {
                Console.WriteLine("Not all runs are identical.");
            }

            string outputPath = Path.Combine(sourceDirectory, "packageTable.csv");
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                packageTable.PrintTable(writer);
            }

            Console.WriteLine($"Results written to {outputPath}.");
        }

        private static void ProcessTrace(string pathToTrace, PackageTable packageTable)
        {
            using (ETWTraceEventSource source = new ETWTraceEventSource(pathToTrace))
            {
                source.Dynamic.AddCallbackForProviderEvent("Microsoft-NuGet", "WalkAsyncTransitivePackage", (data) =>
                {
                    string package = data.PayloadString(0);
                    int transitiveHitCount = (int)data.PayloadValue(1);

                    packageTable.IncrementBy(package, transitiveHitCount);
                });

                source.Process();
            }
        }
    }
}