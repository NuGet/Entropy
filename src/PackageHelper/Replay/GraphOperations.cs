using System;
using System.Collections.Generic;
using System.Linq;

namespace PackageHelper.Replay
{
    class GraphOperations
    {
        public static void LazyTransitiveReduction(RequestGraph graph)
        {
            // Note that this is a partial implementation of transitive reduction. For the request graph generated from
            // logs, transitive dependencies are always lifted to all dependents. This means at any given node, you
            // only need to inspec the direct dependents' dependencies (non-recursively). A more thorough implementation
            // would perform a depth-first search at each node to determine the reduction.
            Console.WriteLine("  Finding dependents...");
            var nodeToDependents = GetNodeToDependents(graph);

            Console.WriteLine("  Reducing transitive dependencies...");
            var beforeCount = graph.Nodes.Sum(x => x.Dependencies.Count);
            foreach (var node in graph.Nodes)
            {
                foreach (var dependent in nodeToDependents[node])
                {
                    foreach (var dependency in node.Dependencies)
                    {
                        if (dependent.Dependencies.Remove(dependency))
                        {
                            nodeToDependents[dependency].Remove(dependent);
                        }
                    }
                }
            }
            var afterCount = graph.Nodes.Sum(x => x.Dependencies.Count);
            Console.WriteLine($"  Dependencies delta: {beforeCount:n0} => {afterCount:n0} ({afterCount - beforeCount:n0})");
        }

        public static void ValidateReferences(RequestGraph graph)
        {
            // Ensure all of the node references are within the graph.
            var references = new HashSet<RequestNode>(graph.Nodes);
            foreach (var node in graph.Nodes)
            {
                foreach (var dependency in node.Dependencies)
                {
                    if (!references.Contains(dependency))
                    {
                        throw new InvalidOperationException("A node dependency was found that is not in the same graph.");
                    }
                }
            }

            // Ensure that each node is unique per hit index + URL combination.
            var unique = new HashSet<RequestNode>(graph.Nodes, CompareByHitIndexAndRequest.Instance);
            if (unique.Count != references.Count)
            {
                throw new InvalidOperationException("There are duplicate nodes in the graph, by hit index and URL.");
            }

            // Ensure that each node is unique in the node list.
            if (graph.Nodes.Count != references.Count)
            {
                throw new InvalidOperationException("There are duplicate nodes in the graph, by reference.");
            }
        }

        public static Dictionary<RequestNode, RequestNode> GetNodeToNode(RequestGraph graph)
        {
            return graph.Nodes.ToDictionary(x => x, CompareByHitIndexAndRequest.Instance);
        }

        public static void Merge(RequestGraph existingGraph, Dictionary<RequestNode, RequestNode> existingNodes, RequestGraph newGraph)
        {
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
                    .ToHashSet(CompareByHitIndexAndRequest.Instance);
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
        }

        public static List<RequestNode> TopologicalSort(RequestGraph graph)
        {
            var nodeToDependents = GetNodeToDependents(graph);
            var nodeToDependencies = GetNodeToDependencies(graph);

            var queue = new Queue<RequestNode>();
            foreach (var pair in nodeToDependents)
            {
                if (pair.Value.Count == 0)
                {
                    queue.Enqueue(pair.Key);
                }
            }

            var topologicalSort = new List<RequestNode>();
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                topologicalSort.Add(node);

                var dependencies = nodeToDependencies[node];
                foreach (var dependency in dependencies.ToList())
                {
                    dependencies.Remove(dependency);

                    var otherDependents = nodeToDependents[dependency];
                    otherDependents.Remove(node);
                    if (otherDependents.Count == 0)
                    {
                        queue.Enqueue(dependency);
                    }
                }
            }

            if (nodeToDependents.Any(x => x.Value.Count > 0)
                || nodeToDependencies.Any(x => x.Value.Count > 0))
            {
                throw new InvalidOperationException("Not all of the edges were removed. There are cycles in the graph.");
            }

            return topologicalSort;
        }

        private static Dictionary<RequestNode, HashSet<RequestNode>> GetNodeToDependencies(RequestGraph graph)
        {
            var nodeToDependencies = new Dictionary<RequestNode, HashSet<RequestNode>>();
            foreach (var node in graph.Nodes)
            {
                nodeToDependencies.Add(node, new HashSet<RequestNode>(node.Dependencies));
            }

            return nodeToDependencies;
        }

        private static Dictionary<RequestNode, HashSet<RequestNode>> GetNodeToDependents(RequestGraph graph)
        {
            var nodeToDependents = new Dictionary<RequestNode, HashSet<RequestNode>>();
            foreach (var node in graph.Nodes)
            {
                if (!nodeToDependents.ContainsKey(node))
                {
                    nodeToDependents.Add(node, new HashSet<RequestNode>());
                }

                foreach (var dependency in node.Dependencies)
                {
                    if (!nodeToDependents.TryGetValue(dependency, out var dependents))
                    {
                        dependents = new HashSet<RequestNode>();
                        nodeToDependents.Add(dependency, dependents);
                    }

                    dependents.Add(node);
                }
            }

            return nodeToDependents;
        }
    }
}
