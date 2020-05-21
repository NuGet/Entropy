using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace PackageHelper.RestoreReplay
{
    static class LogParser
    {
        private static readonly Regex StartRequestRegex = new Regex("^  (?<Method>GET) (?<Url>https?://.+)$");
        private static readonly Regex EndRequestRegex = new Regex("^  (?<StatusCode>OK|NotFound|InternalServerError) (?<Url>https?://.+?) (?<DurationMs>\\d+)ms$");
        private static readonly Regex OtherRequestRegex = new Regex("^\\s*https?://");

        public static List<RestoreRequestGraph> ParseAndMergeRestoreRequestGraphs(string logDir)
        {
            var graphs = new Dictionary<
                (string VariantName, string SolutionName, string SourcesKey),
                (List<string> Sources, RequestGraph Graph, Dictionary<RequestNode, RequestNode> Nodes)>();
            var stringToString = new Dictionary<string, string>();

            foreach (var logPath in Directory.EnumerateFiles(logDir, "restoreLog-*-*.txt"))
            {
                Console.WriteLine($"Parsing {logPath}...");

                // Parse the graph.
                var newGraphInfo = ParseGraph(logPath, stringToString);
                var newGraph = newGraphInfo.Graph;
                var newNodes = GraphOperations.GetNodeToNode(newGraph);

                // Parse the solution name out of the file name.
                var logFileName = Path.GetFileName(logPath);
                var pieces = logFileName.Split(new[] { '-' });
                string variantName;
                string solutionName;
                if (pieces.Length == 3)
                {
                    variantName = null;
                    solutionName = pieces[1];
                }
                else if (pieces.Length == 4)
                {
                    variantName = pieces[1];
                    solutionName = pieces[2];
                }
                else
                {
                    Console.WriteLine("  Skipping, because the file name should have 3 or 4 hyphen separated pieces.");
                    Console.WriteLine("    Format #1 - restoreLog-{solutionName}-{timestamp}.txt");
                    Console.WriteLine("    Format #2 - restoreLog-{variantName}-{solutionName}-{timestamp}.txt");
                    continue;
                }

                // Display statistics.
                if (variantName != null)
                {
                    Console.WriteLine($"  Variant name:       {variantName}");
                }
                Console.WriteLine($"  Solution name:      {solutionName}");
                Console.WriteLine($"  Request count:      {newGraph.Nodes.Count:n0}");
                Console.WriteLine($"  Package sources:");
                foreach (var source in newGraphInfo.Sources)
                {
                    Console.WriteLine($"  - {source}");
                }

                // Find the existing graph with the same solution name and sources.
                var sourcesKey = string.Join(Environment.NewLine, newGraphInfo.Sources.OrderBy(x => x, StringComparer.Ordinal));
                var graphKey = (variantName, solutionName, sourcesKey);
                if (!graphs.TryGetValue(graphKey, out var existingGraphInfo))
                {
                    graphs.Add(graphKey, (newGraphInfo.Sources, newGraph, newNodes));
                    continue;
                }

                var existingGraph = existingGraphInfo.Graph;
                var existingNodes = existingGraphInfo.Nodes;

                Console.WriteLine("Merging with existing graph...");
                GraphOperations.Merge(existingGraph, existingNodes, newGraph);
            }

            var graphInfos = graphs
                .Select(x => new RestoreRequestGraph(x.Key.VariantName, x.Key.SolutionName, x.Value.Sources, x.Value.Graph))
                .ToList();

            // Run consistency checks.
            foreach (var graphInfo in graphInfos)
            {
                GraphOperations.ValidateReferences(graphInfo.Graph);
            }

            return graphInfos;
        }

        public static RestoreRequestGraph ParseGraph(string logPath, Dictionary<string, string> stringToString)
        {
            var pendingRequests = new Dictionary<string, Queue<RequestNode>>();
            var urlToCount = new Dictionary<string, int>();
            var startedRequests = new List<RequestNode>();
            var completedRequests = new HashSet<RequestNode>(HitIndexAndUrlComparer.Instance);
            var currentConcurrency = 0;
            var maxConcurrency = 0;
            List<string> sources = null;

            Parse(
                logPath,
                stringToString,
                startRequest =>
                {
                    if (!urlToCount.TryGetValue(startRequest.Url, out var count))
                    {
                        count = 1;
                        urlToCount.Add(startRequest.Url, count);
                    }
                    else
                    {
                        count++;
                        urlToCount[startRequest.Url] = count;
                    }

                    var requestNode = new RequestNode(count - 1, startRequest, completedRequests);
                    startedRequests.Add(requestNode);

                    currentConcurrency++;
                    maxConcurrency = Math.Max(currentConcurrency, maxConcurrency);

                    if (!pendingRequests.TryGetValue(startRequest.Url, out var pendingNodes))
                    {
                        pendingNodes = new Queue<RequestNode>();
                        pendingRequests.Add(startRequest.Url, pendingNodes);
                    }

                    pendingNodes.Enqueue(requestNode);
                },
                endRequest =>
                {
                    // We assume the first response with the matching URL is associated with the first request. This is
                    // not necessarily true (A-A-B-B vs. A-B-B-A) but we must make an arbitrary decision since the logs
                    // don't have enough information to be certain.
                    var nodes = pendingRequests[endRequest.Url];
                    var requestNode = nodes.Dequeue();
                    requestNode.EndRequest = endRequest;

                    currentConcurrency--;

                    completedRequests.Add(requestNode);
                    
                    if (nodes.Count == 0)
                    {
                        pendingRequests.Remove(endRequest.Url);
                    }
                },
                parsedSources => sources = parsedSources);

            if (sources == null)
            {
                throw new InvalidDataException("No sources were found.");
            }

            return new RestoreRequestGraph(null, null, sources, new RequestGraph(startedRequests));
        }

        private static void Parse(
            string logPath,
            Dictionary<string, string> stringToString,
            Action<StartRequest> onStartRequest,
            Action<EndRequest> onEndRequest,
            Action<List<string>> onSources)
        {
            using (var fileStream = File.OpenRead(logPath))
            using (var streamReader = new StreamReader(fileStream))
            {
                string line;
                var inSourceList = false;
                var sources = new List<string>();
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (inSourceList)
                    {
                        if (!line.StartsWith("    "))
                        {
                            onSources(sources);
                            inSourceList = false;
                        }
                        else
                        {
                            sources.Add(DedupeString(stringToString, line.Trim()));
                        }
                    }
                    else if (TryParseStartRequest(line, stringToString, out var startRequest))
                    {
                        onStartRequest(startRequest);
                    }
                    else if (TryParseEndRequest(line, stringToString, out var endRequest))
                    {
                        onEndRequest(endRequest);
                    }
                    else if (line == "Feeds used:")
                    {
                        inSourceList = true;
                    }
                    else if (OtherRequestRegex.IsMatch(line))
                    {
                        throw new InvalidDataException("Unexpected request line: " + line);
                    }
                }

                if (inSourceList)
                {
                    onSources(sources);
                }
            }
        }
        
        private static bool TryParseStartRequest(string line, Dictionary<string, string> stringToString, out StartRequest startRequest)
        {
            var match = StartRequestRegex.Match(line);
            if (match.Success)
            {
                startRequest = new StartRequest(
                    DedupeString(stringToString, match.Groups["Method"].Value),
                    DedupeString(stringToString, match.Groups["Url"].Value.Trim()));
                return true;
            }

            startRequest = null;
            return false;
        }

        private static bool TryParseEndRequest(string line, Dictionary<string, string> stringToString, out EndRequest endRequest)
        {
            var match = EndRequestRegex.Match(line);
            if (match.Success)
            {
                endRequest = new EndRequest(
                    (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), match.Groups["StatusCode"].Value),
                    DedupeString(stringToString, match.Groups["Url"].Value.Trim()),
                    TimeSpan.FromMilliseconds(int.Parse(match.Groups["DurationMs"].Value)));
                return true;
            }

            endRequest = null;
            return false;
        }

        private static string DedupeString(Dictionary<string, string> stringToString, string input)
        {
            if (!stringToString.TryGetValue(input, out var existingUrl))
            {
                stringToString.Add(input, input);
                return input;
            }
            else
            {
                return existingUrl;
            }
        }
    }
}
