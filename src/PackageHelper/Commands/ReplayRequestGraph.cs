using System;
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
using PackageHelper.Replay;
using PackageHelper.Replay.Requests;

namespace PackageHelper.Commands
{
    static class ReplayRequestGraph
    {
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

            command.Handler = CommandHandler.Create<string, int, int>(ExecuteAsync);

            return command;
        }

        static async Task<int> ExecuteAsync(string path, int iterations, int maxConcurrency)
        {
            if (!Helper.TryFindRoot(out var rootDir))
            {
                return 1;
            }

            Console.WriteLine("Parsing the file name...");
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - 5);
            }
            var pieces = fileName.Split('-');

            string variantName;
            string solutionName;
            if (pieces.Length == 2)
            {
                variantName = null;
                solutionName = pieces[1];
            }
            else if (pieces.Length >= 3)
            {
                variantName = pieces[1];
                solutionName = pieces[2];
            }
            else
            {
                variantName = null;
                solutionName = null;
            }

            Console.WriteLine($"  Variant name:  {variantName ?? "(none)"}");
            Console.WriteLine($"  Solution name: {solutionName ?? "(none)"}");

            Console.WriteLine($"Reading {path}...");
            var graph = RequestGraphSerializer.ReadFromFile(path);
            Console.WriteLine($"There are {graph.Nodes.Count} requests in the graph.");

            var resultsPath = Path.Combine(rootDir, "out", "replay-results.csv");
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
                        iteration);
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
            int iteration)
        {
            var logPrefix = $"[{iteration}/{iterations}{(iteration == 0 ? " (warm-up)" : string.Empty)}]";

            string requestsFileName;
            if (variantName == null)
            {
                requestsFileName = $"replayLog-{solutionName}-{Helper.GetLogTimestamp()}.csv";
            }
            else
            {
                requestsFileName = $"replayLog-{variantName}-{solutionName}-{Helper.GetLogTimestamp()}.csv";
            }

            var requestsPath = Path.Combine(rootDir, "out", "logs", requestsFileName);

            Console.WriteLine($"{logPrefix} Starting...");
            var stopwatch = Stopwatch.StartNew();
            var nodeToTask = new Dictionary<RequestNode, Task>();
            var throttle = new SemaphoreSlim(maxConcurrency);
            var consoleLock = new object();

            using (var writer = new RequestResultWriter(requestsPath))
            {
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

            Helper.AppendCsv(resultsPath, new CsvRecord
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
            });
        }

        private static async Task GetRequestTask(
            Dictionary<RequestNode, Task> nodeToTask,
            SemaphoreSlim throttle,
            object consoleLock,
            HttpClient httpClient,
            RequestNode node,
            IRequestResultWriter writer)
        {
            await Task.WhenAll(node.Dependencies.Select(n => nodeToTask[n]).ToList());

            await throttle.WaitAsync();
            try
            {
                if (node.EndRequest == null)
                {
                    return;
                }

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

                            writer.OnResponse(node, response.StatusCode, headerDuration, stopwatch.Elapsed - headerDuration);

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

        private class CsvRecord
        {
            public string TimestampUtc { get; set; }
            public string MachineName { get; set; }
            public int Iteration { get; set; }
            public bool IsWarmUp { get; set; }
            public int Iterations { get; set; }
            public string VariantName { get; set; }
            public string SolutionName { get; set; }
            public object RequestCount { get; set; }
            public double DurationMs { get; set; }
            public int MaxConcurrency { get; set; }
            public string LogFileName { get; set; }
        }
    }
}
