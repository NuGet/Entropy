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

        public static async Task<int> ExecuteAsync(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine($"The {Name} command requires a request graph (e.g. a requestGraph-*.json.gz file) as the argument.");
                return 1;
            }

            var path = args[0];
            Console.WriteLine($"Reading {path}...");
            var graph = RequestGraphSerializer.ReadFromFile(path);

            using (var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
            using (var httpClient = new HttpClient(handler))
            {
                var maxConcurrency = 64;
                var iterations = 1;

                await ExecuteRequestsAsync(graph, httpClient, maxConcurrency, iterations);
            }

            return 0;
        }

        private static async Task ExecuteRequestsAsync(RequestGraph graph, HttpClient httpClient, int maxConcurrency, int iterations)
        {
            Console.WriteLine("Sorting the requests in topological order...");
            var topologicalOrder = GraphOperations.TopologicalSort(graph);
            topologicalOrder.Reverse();

            for (var i = 0; i < iterations; i++)
            {
                Console.WriteLine($"[{i}/{iterations}] Starting {topologicalOrder.Count} requests...");
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

                Console.WriteLine($"[{i}/{iterations}] Completed {topologicalOrder.Count} requests in {stopwatch.ElapsedMilliseconds}ms.");
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
                        Console.WriteLine($"  {node.StartRequest.Method} {node.StartRequest.Url}");
                        using (var response = await httpClient.GetAsync(node.StartRequest.Url, HttpCompletionOption.ResponseHeadersRead))
                        {
                            Console.WriteLine($"  HEADERS {response.StatusCode} {node.StartRequest.Url} {stopwatch.ElapsedMilliseconds}ms");
                            using (var stream = await response.Content.ReadAsStreamAsync())
                            {
                                int read;
                                do
                                {
                                    read = await stream.ReadAsync(buffer, 0, buffer.Length);
                                }
                                while (read > 0);
                            }

                            Console.WriteLine($"  BODY {response.StatusCode} {node.StartRequest.Url} {stopwatch.ElapsedMilliseconds}ms");
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
