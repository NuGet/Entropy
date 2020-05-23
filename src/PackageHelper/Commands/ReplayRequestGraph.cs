using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using PackageHelper.RestoreReplay;

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
            Console.WriteLine($"Reading {path}...");
            var graph = RequestGraphSerializer.ReadFromFile(path);
            Console.WriteLine($"There are {graph.Nodes.Count} requests in the graph.");

            using (var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
            using (var httpClient = new HttpClient(handler))
            {
                await ExecuteRequestsAsync(graph, httpClient, iterations, maxConcurrency);
            }

            return 0;
        }

        private static async Task ExecuteRequestsAsync(RequestGraph graph, HttpClient httpClient, int iterations, int maxConcurrency)
        {
            Console.WriteLine("Sorting the requests in topological order...");
            var topologicalOrder = GraphOperations.TopologicalSort(graph);
            topologicalOrder.Reverse();

            for (var i = 0; i <= iterations; i++)
            {
                var logPrefix = $"[{i}/{iterations}{(i == 0 ? " (warm-up)" : string.Empty)}]";
                Console.WriteLine($"{logPrefix} Starting...");
                var stopwatch = Stopwatch.StartNew();
                var nodeToTask = new Dictionary<RequestNode, Task>();
                var throttle = new SemaphoreSlim(maxConcurrency);
                var consoleLock = new object();

                foreach (var node in topologicalOrder)
                {
                    nodeToTask.Add(node, GetRequestTask(nodeToTask, throttle, consoleLock, httpClient, node));
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

                Console.WriteLine($"{logPrefix} Completed in {stopwatch.ElapsedMilliseconds}ms.");
            }
        }

        private static async Task GetRequestTask(
            Dictionary<RequestNode, Task> nodeToTask,
            SemaphoreSlim throttle,
            object consoleLock,
            HttpClient httpClient,
            RequestNode node)
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
                            using (var stream = await response.Content.ReadAsStreamAsync())
                            {
                                int read;
                                do
                                {
                                    read = await stream.ReadAsync(buffer, 0, buffer.Length);
                                }
                                while (read > 0);
                            }
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
