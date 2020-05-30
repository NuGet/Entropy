using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using PackageHelper.Csv;
using PackageHelper.Replay;
using PackageHelper.Replay.Operations;
using PackageHelper.Replay.Requests;

namespace PackageHelper.Commands
{
    class DumpRequestDurations
    {
        public static Command GetCommand()
        {
            var command = new Command("dump-request-durations")
            {
                Description = "Parse logs for request durations and write them to a gzipped CSV",
            };

            command.Add(new Option<string>("log-dir")
            {
                Description = "The directory to scan for recognized logs",
            });
            command.Add(new Option<string>("path")
            {
                Description = "The file path to write the request durations to (should end in .csv.gz)",
            });
            command.Add(new Option<string>("machine-name", () => Environment.MachineName)
            {
                Description = "The machine name to set for request logs with unknown machine name",
            });
            command.Add(new Option("--sources")
            {
                Description = "Package sources to use for operation parsing",
                Argument = new Argument
                {
                    Arity = ArgumentArity.OneOrMore,
                },
            });
            command.Add(new Option("--exclude-variants")
            {
                Description = "Variants to exclude from processing",
                Argument = new Argument
                {
                    Arity = ArgumentArity.OneOrMore,
                },
            });
            command.Add(new Option<bool>("--warnings-as-errors")
            {
                Description = "Stop on the first warning.",
            });

            command.Handler = CommandHandler.Create<string, string, string, List<string>, List<string>, bool>(ExecuteAsync);

            return command;
        }


        private static async Task<int> ExecuteAsync(
            string logDir,
            string path,
            string machineName,
            List<string> sources,
            List<string> excludeVariants,
            bool warningsAsErrors)
        {
            var ctx = new Context(logDir, path, machineName, excludeVariants, warningsAsErrors);

            if (!InitializePaths(ctx))
            {
                return 1;
            }

            await UpdateSources(ctx, sources);

            return await ExecuteAsync(ctx);
        }

        private static bool InitializePaths(Context ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.LogDir) || string.IsNullOrWhiteSpace(ctx.Path))
            {
                if (!Helper.TryFindRoot(out var rootDir))
                {
                    return false;
                }

                var outDir = Path.Combine(rootDir, "out");
                if (string.IsNullOrWhiteSpace(ctx.LogDir))
                {
                    ctx.LogDir = Path.Combine(outDir, "logs");
                }

                if (string.IsNullOrWhiteSpace(ctx.Path))
                {
                    ctx.Path = Path.Combine(outDir, "request-durations.csv.gz");
                }

                if (Directory.Exists(outDir))
                {
                    PopulateRestoreResultLookup(ctx, outDir);
                    PopulateReplayResultLookup(ctx, outDir);
                }
            }

            Console.WriteLine($"Log directory: {ctx.LogDir}");
            Console.WriteLine($"Output path:   {ctx.Path}");

            var dir = Path.GetDirectoryName(ctx.Path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return true;
        }

        private static async Task UpdateSources(Context ctx, IEnumerable<string> sources)
        {
            var newSources = sources.Except(ctx.Sources).ToList();
            if (newSources.Any())
            {
                ctx.Sources.AddRange(newSources);
                ctx.Sources.Sort(StringComparer.Ordinal);

                Console.WriteLine("Loading sources for operation parsing...");
                foreach(var source in sources)
                {
                    Console.WriteLine($"- {source}");
                }

                ctx.OperationParserContext = await OperationParserContext.CreateAsync(ctx.Sources);
            }
        }

        private static void PopulateReplayResultLookup(Context ctx, string outDir)
        {
            var replayResultsPath = Path.Combine(outDir, ReplayRequestGraph.ResultFileName);
            if (File.Exists(replayResultsPath))
            {
                Console.WriteLine("Parsing the replay results for metadata...");
                using (var reader = new StreamReader(replayResultsPath))
                using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    foreach (var record in csvReader.GetRecords<ReplayResultRecord>())
                    {
                        string variantName = null;
                        if (!string.IsNullOrWhiteSpace(record.VariantName))
                        {
                            variantName = record.VariantName;

                            if (ctx.ExcludeVariants.Contains(variantName))
                            {
                                continue;
                            }
                        }

                        var key = (variantName, record.SolutionName, record.LogFileName);
                        if (ctx.ReplayResultLookup.ContainsKey(key))
                        {
                            ctx.WarningCount++;
                            Console.WriteLine($"  WARNING: Duplicate replay result: {variantName}, {record.SolutionName}, {record.LogFileName}");
                        }
                        else
                        {
                            ctx.ReplayResultLookup.Add(key, record);
                        }
                    }
                }

                Console.WriteLine($"{ctx.ReplayResultLookup.Count} replay results were found.");
            }
        }

        private static void PopulateRestoreResultLookup(Context ctx, string outDir)
        {
            Console.WriteLine("Parsing restore result files for metadata...");
            var fileCount = 0;
            foreach (var resultPath in Directory.EnumerateFiles(outDir, "results-*.csv"))
            {
                fileCount++;
                using (var streamReader = new StreamReader(resultPath))
                using (var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture))
                {
                    foreach (var record in csvReader.GetRecords<RestoreResultRecord>())
                    {
                        string variantName = null;
                        if (!string.IsNullOrWhiteSpace(record.VariantName))
                        {
                            variantName = record.VariantName;

                            if (ctx.ExcludeVariants.Contains(variantName))
                            {
                                continue;
                            }
                        }

                        var key = (variantName, record.SolutionName, record.LogFileName);
                        if (ctx.RestoreResultLookup.ContainsKey(key))
                        {
                            ctx.WarningCount++;
                            Console.WriteLine($"  WARNING: Duplicate restore result: {variantName}, {record.SolutionName}, {record.LogFileName}");
                        }
                        else
                        {
                            ctx.RestoreResultLookup.Add(key, record);
                        }
                    }
                }
            }

            Console.WriteLine($"{fileCount} restore result files were parsed. {ctx.RestoreResultLookup.Count} restore results were found.");
        }

        private static async Task<int> ExecuteAsync(Context ctx)
        {
            using (var writer = new BackgroundCsvWriter<RequestDurationRecord>(ctx.Path, gzip: true))
            {
                foreach (var graph in RestoreLogParser.ParseGraphs(ctx.LogDir, ctx.ExcludeVariants, ctx.StringToString))
                {
                    await UpdateSources(ctx, graph.Graph.Sources);

                    WriteGraph(ctx, writer, graph);
                }

                foreach (var replayLogPath in Directory.EnumerateFiles(ctx.LogDir, $"{ReplayRequestGraph.ReplayLogPrefix}-*-*.csv"))
                {
                    WriteReplayLog(ctx, writer, replayLogPath);
                }
            }

            return ctx.WarningsAsErrors && ctx.WarningCount > 0 ? 1 : 0;
        }

        private static void WriteReplayLog(Context ctx, BackgroundCsvWriter<RequestDurationRecord> writer, string replayLogPath)
        {
            var fileName = Path.GetFileName(replayLogPath);
            if (!Helper.TryParseFileName(replayLogPath, out var fileType, out var variantName, out var solutionName)
                || fileType != ReplayRequestGraph.ReplayLogPrefix)
            {
                return;
            }

            if (ctx.ExcludeVariants.Contains(variantName))
            {
                return;
            }

            Console.WriteLine($"Parsing {replayLogPath}...");

            var hasRecord = ctx.ReplayResultLookup.TryGetValue(
                (variantName, solutionName, fileName),
                out var replayResultRecord);

            if (variantName != null)
            {
                Console.WriteLine($"  Variant name:  {variantName}");
            }
            Console.WriteLine($"  Solution name: {solutionName}");

            if (!hasRecord)
            {
                ctx.WarningCount++;
                Console.WriteLine($"  WARNING: No metadata found for replay log: ({variantName}, {solutionName}, {fileName})");
            }

            using (var reader = new StreamReader(replayLogPath))
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                int requestIndex = 0;
                foreach (var record in csvReader.GetRecords<ReplayRequestRecord>())
                {
                    const string method = "GET";
                    var operationInfo = GetOperationInfo(ctx, new StartRequest(method, record.Url));

                    writer.Add(new RequestDurationRecord
                    {
                        VariantName = variantName,
                        SolutionName = solutionName,
                        RequestType = RequestType.Replay,
                        MachineName = replayResultRecord?.MachineName ?? ctx.MachineName,
                        LogFileIndex = ctx.LogFileIndex,
                        LogFileRequestIndex = requestIndex,
                        IsWarmUp = hasRecord ? replayResultRecord.IsWarmUp : (bool?)null,
                        Iteration = hasRecord ? replayResultRecord.Iteration : (int?)null,
                        Iterations = hasRecord ? replayResultRecord.Iterations : (int?)null,
                        Method = method,
                        Url = record.Url,
                        StatusCode = record.StatusCode,
                        HeaderDurationMs = record.HeaderDurationMs,
                        BodyDurationMs = record.BodyDurationMs,
                        OperationType = operationInfo?.Operation.Type,
                        PackageId = (operationInfo?.Operation as OperationWithId)?.Id,
                        PackageVersion = (operationInfo?.Operation as OperationWithIdVersion)?.Version,
                    });

                    requestIndex++;
                }

                Console.WriteLine($"  Request count: {requestIndex:n0}");
            }

            ctx.LogFileIndex++;
        }

        private static void WriteGraph(Context ctx, BackgroundCsvWriter<RequestDurationRecord> writer, RestoreRequestGraph graph)
        {
            if (ctx.ExcludeVariants.Contains(graph.VariantName))
            {
                return;
            }

            var hasRecord = ctx.RestoreResultLookup.TryGetValue(
                (graph.VariantName, graph.SolutionName, graph.FileName),
                out var restoreResultRecord);

            if (!hasRecord)
            {
                ctx.WarningCount++;
                Console.WriteLine($"  WARNING: No metadata found for restore log: ({graph.VariantName}, {graph.SolutionName}, {graph.FileName})");
            }

            int requestIndex = 0;
            foreach (var node in graph.Graph.Nodes)
            {
                if (node.EndRequest == null)
                {
                    continue;
                }

                var operationInfo = GetOperationInfo(ctx, node.StartRequest);

                writer.Add(new RequestDurationRecord
                {
                    VariantName = graph.VariantName,
                    SolutionName = graph.SolutionName,
                    RequestType = RequestType.Restore,
                    MachineName = restoreResultRecord?.MachineName ?? ctx.MachineName,
                    LogFileIndex = ctx.LogFileIndex,
                    LogFileRequestIndex = requestIndex,
                    IsWarmUp = hasRecord ? restoreResultRecord.ScenarioName == "warmup" : (bool?)null,
                    Iteration = hasRecord ? restoreResultRecord.Iteration : (int?)null,
                    Iterations = hasRecord ? restoreResultRecord.IterationCount : (int?)null,
                    Method = node.StartRequest.Method,
                    Url = node.StartRequest.Url,
                    StatusCode = (int)node.EndRequest.StatusCode,
                    HeaderDurationMs = node.EndRequest.Duration.TotalMilliseconds,
                    BodyDurationMs = null,
                    OperationType = operationInfo?.Operation.Type,
                    PackageId = (operationInfo?.Operation as OperationWithId)?.Id,
                    PackageVersion = (operationInfo?.Operation as OperationWithIdVersion)?.Version,
                });

                requestIndex++;
            }

            ctx.LogFileIndex++;
        }

        private static OperationInfo GetOperationInfo(Context ctx, StartRequest request)
        {
            if (!ctx.RequestToOperation.TryGetValue(request, out var operationInfo))
            {
                operationInfo = OperationParser.Parse(ctx.OperationParserContext, request);

                if (operationInfo.Operation == null)
                {
                    ctx.WarningCount++;
                    TryWarning(
                        $"  WARNING: Unknown operation: {request.Method} {request.Url}",
                        ref ctx.UnknownOperationWarnings,
                        "No more unknown operation warnings will be shown. Specify sources to resolve them.");
                }

                ctx.RequestToOperation.Add(request, operationInfo);
            }

            return operationInfo;
        }

        private static void TryWarning(string warning, ref int counter, string maxWarning)
        {
            const int maxWarnings = 10;
            if (counter < maxWarnings)
            {
                counter++;
                Console.WriteLine($"  WARNING: {warning}");
                if (counter == maxWarnings)
                {
                    Console.WriteLine($"  {maxWarning}");
                }
            }
        }

        private class Context
        {
            public Context(string logDir, string path, string machineName, IEnumerable<string> excludeVariants, bool warningsAsErrors)
            {
                LogDir = logDir;
                Path = path;
                MachineName = machineName;
                ExcludeVariants = excludeVariants.ToHashSet();
                WarningsAsErrors = warningsAsErrors;

                Sources = new List<string>();

                RestoreResultLookup = new Dictionary<(string variantName, string solutionName, string logFileName), RestoreResultRecord>();
                ReplayResultLookup = new Dictionary<(string variantName, string solutionName, string logFileName), ReplayResultRecord>();
                StringToString = new Dictionary<string, string>();
                RequestToOperation = new Dictionary<StartRequest, OperationInfo>();
            }

            public string LogDir { get; set; }
            public string Path { get; set; }
            public string MachineName { get; set; }
            public HashSet<string> ExcludeVariants { get; }
            public bool WarningsAsErrors { get; }
            public List<string> Sources { get; }
            public Dictionary<(string variantName, string solutionName, string logFileName), RestoreResultRecord> RestoreResultLookup { get; }
            public Dictionary<(string variantName, string solutionName, string logFileName), ReplayResultRecord> ReplayResultLookup { get; }
            public Dictionary<string, string> StringToString { get; }
            public OperationParserContext OperationParserContext { get; set; }
            public Dictionary<StartRequest, OperationInfo> RequestToOperation { get; }
            public int LogFileIndex { get; set; }
            public int WarningCount { get; set; }

            public int UnknownOperationWarnings;
        }
    }
}
