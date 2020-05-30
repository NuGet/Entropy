﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using PackageHelper.Csv;
using PackageHelper.Replay;
using PackageHelper.Replay.Requests;

namespace PackageHelper.Commands
{
    static class ReplayRequestGraph
    {
        public const string ResultFileName = "replay-results.csv";
        public const string ReplayLogPrefix = "replayLog";

        public static Command GetCommand()
        {
            var command = new Command("replay-request-graph")
            {
                Description = "Replay a specified request graph to measure performance",
            };

            command.Add(new Argument<string>("path")
            {
                Description = "Path to a serialized request graph (requestGraph-*-*.json.gz file) to replay",
            });
            command.Add(new Option<int>(
                "--iterations",
                getDefaultValue: () => 20)
            {
                Description = "Number of times to replay the request graph before terminating"
            });
            command.Add(new Option<int>(
                "--max-concurrency",
                getDefaultValue: () => 64)
            {
                Description = "Max concurrency for HTTP requests"
            });
            command.Add(new Option<bool>("--no-dependencies")
            {
                Description = "Run the requests as if they have no dependencies"
            });

            command.Handler = CommandHandler.Create<string, int, int, bool>(ExecuteAsync);

            return command;
        }

        static async Task<int> ExecuteAsync(string path, int iterations, int maxConcurrency, bool noDependencies)
        {
            if (!Helper.TryFindRoot(out var rootDir))
            {
                return 1;
            }

            if (!path.EndsWith(GraphSerializer.FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"The serialized graph must have the extension {GraphSerializer.FileExtension}");
                return 1;
            }

            Console.WriteLine("Parsing the file name...");
            if (!Helper.TryParseFileName(path, out var graphType, out var variantName, out var solutionName))
            {
                graphType = null;
                variantName = null;
                solutionName = null;
            }

            Console.WriteLine($"  Graph type:    {graphType ?? "(none)"}");
            Console.WriteLine($"  Variant name:  {variantName ?? "(none)"}");
            Console.WriteLine($"  Solution name: {solutionName ?? "(none)"}");

            if (graphType != RequestGraph.Type)
            {
                Console.WriteLine($"The input graph type must be {RequestGraph.Type}.");
                return 1;
            }

            Console.WriteLine($"Reading {path}...");
            var graph = RequestGraphSerializer.ReadFromFile(path);
            Console.WriteLine($"There are {graph.Nodes.Count} nodes in the graph.");
            Console.WriteLine($"There are {graph.Nodes.Sum(x => x.Dependencies.Count)} edges in the graph.");

            if (noDependencies)
            {
                Console.WriteLine("Clearing dependencies...");
                foreach (var node in graph.Nodes)
                {
                    node.Dependencies.Clear();
                }
            }

            var resultsPath = Path.Combine(rootDir, "out", ResultFileName);
            Console.WriteLine($"Results will be writen to {resultsPath}.");

            using (var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
            using (var httpClient = new HttpClient(handler))
            {
                Console.WriteLine("Sorting the requests in topological order...");
                var topologicalOrder = GraphOperations.TopologicalSort(graph);
                topologicalOrder.Reverse();

                for (var iteration = 0; iteration <= iterations; iteration++)
                {
                    await ExecuteIterationAsync(
                        rootDir,
                        resultsPath,
                        variantName,
                        solutionName,
                        httpClient,
                        iterations,
                        maxConcurrency,
                        topologicalOrder,
                        iteration,
                        noDependencies);
                }
            }

            return 0;
        }

        private static async Task ExecuteIterationAsync(
            string rootDir,
            string resultsPath,
            string variantName,
            string solutionName,
            HttpClient httpClient,
            int iterations,
            int maxConcurrency,
            List<RequestNode> topologicalOrder,
            int iteration,
            bool noDependencies)
        {
            var logPrefix = $"[{iteration}/{iterations}{(iteration == 0 ? " (warm-up)" : string.Empty)}]";

            string requestsFileName;
            if (variantName == null)
            {
                requestsFileName = $"{ReplayLogPrefix}-{solutionName}-{Helper.GetLogTimestamp()}.csv";
            }
            else
            {
                requestsFileName = $"{ReplayLogPrefix}-{variantName}-{solutionName}-{Helper.GetLogTimestamp()}.csv";
            }

            var logsDir = Path.Combine(rootDir, "out", "logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            
            var requestsPath = Path.Combine(logsDir, requestsFileName);

            var stopwatch = new Stopwatch();
            using (var writer = new BackgroundCsvWriter<ReplayRequestRecord>(requestsPath, gzip: false))
            {
                Console.WriteLine($"{logPrefix} Starting...");
                var nodeToTask = new Dictionary<RequestNode, Task>();
                var throttle = new SemaphoreSlim(maxConcurrency);
                var consoleLock = new object();

                stopwatch.Start();

                foreach (var node in topologicalOrder)
                {
                    nodeToTask.Add(node, GetRequestTask(nodeToTask, throttle, consoleLock, httpClient, node, writer));
                }

                try
                {
                    await Task.WhenAll(nodeToTask.Values);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("At least one request failed.");
                    Console.WriteLine(ex.ToString());
                }

                stopwatch.Stop();

                Console.WriteLine($"{logPrefix} Completed in {stopwatch.ElapsedMilliseconds}ms.");
            }

            CsvUtility.Append(resultsPath, new ReplayResultRecord
            {
                TimestampUtc = Helper.GetExcelTimestamp(DateTimeOffset.UtcNow),
                MachineName = Environment.MachineName,
                Iteration = iteration,
                IsWarmUp = iteration == 0,
                Iterations = iterations,
                VariantName = variantName,
                SolutionName = solutionName,
                RequestCount = topologicalOrder.Count,
                DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                MaxConcurrency = maxConcurrency,
                LogFileName = requestsFileName,
                NoDependencies = noDependencies,
            });
        }

        private static async Task GetRequestTask(
            Dictionary<RequestNode, Task> nodeToTask,
            SemaphoreSlim throttle,
            object consoleLock,
            HttpClient httpClient,
            RequestNode node,
            BackgroundCsvWriter<ReplayRequestRecord> writer)
        {
            if (node.Dependencies.Any())
            {
                await Task.WhenAll(node.Dependencies.Select(n => nodeToTask[n]).ToList());
            }

            await throttle.WaitAsync();
            try
            {
                if (node.StartRequest.Method != "GET")
                {
                    throw new InvalidOperationException("Only GET requests are supported.");
                }

                var buffer = new byte[80 * 1024];
                const int maxAttempts = 3;
                for (var i = 0; i < maxAttempts; i++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        using (var response = await httpClient.GetAsync(node.StartRequest.Url, HttpCompletionOption.ResponseHeadersRead))
                        {
                            var headerDuration = stopwatch.Elapsed;

                            using (var stream = await response.Content.ReadAsStreamAsync())
                            {
                                int read;
                                do
                                {
                                    read = await stream.ReadAsync(buffer, 0, buffer.Length);
                                }
                                while (read > 0);
                            }

                            writer.Add(new ReplayRequestRecord
                            {
                                Url = node.StartRequest.Url,
                                StatusCode = (int)response.StatusCode,
                                HeaderDurationMs = headerDuration.TotalMilliseconds,
                                BodyDurationMs = (stopwatch.Elapsed - headerDuration).TotalMilliseconds,
                            });

                            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                            {
                                response.EnsureSuccessStatusCode();
                            }

                            break;
                        }
                    }
                    catch (Exception ex) when (i < maxAttempts - 1)
                    {
                        lock (consoleLock)
                        {
                            Console.WriteLine($"  ERROR {node.StartRequest.Url} {stopwatch.ElapsedMilliseconds}ms");
                            Console.WriteLine(ExceptionUtilities.DisplayMessage(ex, indent: true));
                        }
                    }
                }
            }
            finally
            {
                throttle.Release();
            }
        }
    }
}
