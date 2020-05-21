﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace RestoreReplay
{
    class Program
    {
        static int Main(string[] args)
        {
            if (!PackageHelper.Helper.TryFindRoot(out var rootDir))
            {
                return 1;
            }

            var logDir = Path.Combine(rootDir, @"out\logs");
            var graphInfos = LogParser.ParseGraphs(logDir);
            for (int index = 0; index < graphInfos.Count; index++)
            {
                var graphInfo = graphInfos[index];
                var fileName = $"{index}-{graphInfo.SolutionName}.json";

                Console.WriteLine($"Preparing {fileName}...");
                GraphOperations.LazyTransitiveReduction(graphInfo.Graph);

                var filePath = Path.Combine(rootDir, "out", "request-graphs", fileName);
                var outDir = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(outDir);
                Console.WriteLine($"  Writing {filePath}...");
                RequestGraphSerializer.WriteToFile(filePath, graphInfo.Graph);
            }

            return 0;
        }
    }
}
