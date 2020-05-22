using System;
using System.Collections.Generic;
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
        public const string Name = "replay-request-graph";
        private const int DefaultIterationCount = 5;

        public static async Task<int> ExecuteAsync(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine($"The {Name} command requires a request graph (e.g. a requestGraph-*.json.gz file) as the first argument.");
                return 1;
            }

            var path = args[0];

            var iterations = DefaultIterationCount;
            if (args.Length > 1)
            {
                if (!int.TryParse(args[1], out iterations))
                {
                    iterations = DefaultIterationCount;
                    Console.WriteLine($"The second argument for the {Name} command was ignored because it's not an integer.");
                }
                else
                {
                    Console.WriteLine($"The iteration count argument of {iterations} will be used.");
                }
            }
            else
            {
                Console.WriteLine($"Using the default iteration count of {iterations} will be used.");
            }

            Console.WriteLine($"Reading {path}...");
            var graph = RequestGraphSerializer.ReadFromFile(path);
            Console.WriteLine($"There are {graph.Nodes.Count} requests in the graph.");

            using (var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
            using (var httpClient = new HttpClient(handler))
            {
                var maxConcurrency = 64;
                await ExecuteRequestsAsync(graph, httpClient, maxConcurrency, iterations);
            }

            return 0;
        }

        private static async Task ExecuteRequestsAsync(RequestGraph graph, HttpClient httpClient, int maxConcurrency, int iterations)
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
