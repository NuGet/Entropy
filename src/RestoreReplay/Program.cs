using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            var solutionNameToSourcesToGraphAndNodes = new Dictionary<string, Dictionary<string, (RequestGraph Graph, Dictionary<RequestNode, RequestNode> Nodes)>>();
            var stringToString = new Dictionary<string, string>();

            foreach (var logPath in Directory.EnumerateFiles(logDir, "restoreLog-*-*.txt"))
            {
                Console.WriteLine($"Parsing {logPath}...");

                // Parse the graph.
                var newGraph = LogParser.ParseGraph(logPath, stringToString);
                var newNodes = newGraph.Nodes.ToDictionary(x => x, HitIndexAndUrlComparer.Instance);

                // Find the existing graph with the same solution name and sources.
                var logFileName = Path.GetFileName(logPath);
                var pieces = logFileName.Split(new[] { '-' }, 3);
                var solutionName = pieces[1];

                // Display statistics.
                Console.WriteLine($"  Solution name:      {solutionName}");
                Console.WriteLine($"  Request count:      {newGraph.Nodes.Count:n0}");
                Console.WriteLine($"  Max concurrency:    {newGraph.MaxConcurrency:n0}");
                Console.Write($"  Package sources:    {new Uri(newGraph.Sources[0], UriKind.Absolute).Host}");
                if (newGraph.Sources.Count == 1)
                {
                    Console.WriteLine();
                }
                else if (newGraph.Sources.Count == 2)
                {
                    Console.WriteLine(" and 1 other source");
                }
                else
                {
                    Console.WriteLine($" and {newGraph.Sources.Count - 1} other sources");
                }

                if (!solutionNameToSourcesToGraphAndNodes.TryGetValue(solutionName, out var sourcesToGraphAndNodes))
                {
                    sourcesToGraphAndNodes = new Dictionary<string, (RequestGraph graph, Dictionary<RequestNode, RequestNode> nodes)>();
                    solutionNameToSourcesToGraphAndNodes.Add(solutionName, sourcesToGraphAndNodes);
                }

                var sourcesKey = string.Join(Environment.NewLine, newGraph.Sources.OrderBy(x => x, StringComparer.Ordinal));
                if (!sourcesToGraphAndNodes.TryGetValue(sourcesKey, out var existingGraphAndNodes))
                {
                    sourcesToGraphAndNodes.Add(sourcesKey, (newGraph, newNodes));
                    continue;
                }

                Console.WriteLine("Merging with existing graph...");

                var existingGraph = existingGraphAndNodes.Graph;
                var existingNodes = existingGraphAndNodes.Nodes;

                // Add missing nodes.
                var addedNodes = new List<RequestNode>();
                foreach (var newNode in newGraph.Nodes)
                {
                    if (!existingNodes.TryGetValue(newNode, out var existingNode))
                    {
                        existingGraph.Nodes.Add(newNode);
                        existingNodes.Add(newNode, newNode);
                        addedNodes.Add(newNode);
                    }
                }

                // Change the references in the added nodes to point to the existing nodes, not the new nodes.
                foreach (var addedNode in addedNodes)
                {
                    addedNode.Dependencies = addedNode
                        .Dependencies
                        .Select(x => existingNodes[x])
                        .ToHashSet(HitIndexAndUrlComparer.Instance);
                }
                Console.WriteLine($"  New requests:       {addedNodes.Count:n0}");

                var totalDependenciesBefore = existingGraph.Nodes.Sum(x => x.Dependencies.Count);
                var addedEndRequests = 0;
                foreach (var newNode in newGraph.Nodes)
                {
                    var existingNode = existingNodes[newNode];

                    // Change dependencies to be the intersection of the new and existing graphs.
                    existingNode.Dependencies.IntersectWith(newNode.Dependencies);

                    // Fill in missing end request information.
                    if (existingNode.EndRequest == null && newNode.EndRequest != null)
                    {
                        addedEndRequests++;
                        existingNode.EndRequest = newNode.EndRequest;
                    }
                }
                var totalDependenciesAfter = existingGraph.Nodes.Sum(x => x.Dependencies.Count);
                Console.WriteLine($"  New responses:      {addedEndRequests:n0}");
                Console.WriteLine($"  Dependencies delta: {totalDependenciesBefore:n0} => {totalDependenciesAfter:n0} ({totalDependenciesAfter - totalDependenciesBefore:n0})");

                // Use the greater of the two max concurrencies.
                existingGraph.MaxConcurrency = Math.Max(existingGraph.MaxConcurrency, newGraph.MaxConcurrency);
            }

            return 0;
        }
    }
}
