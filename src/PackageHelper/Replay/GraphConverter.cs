using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PackageHelper.Replay.Operations;
using PackageHelper.Replay.Requests;

namespace PackageHelper.Replay
{
    static class GraphConverter
    {
        public static async Task<RequestGraph> ToRequestGraphAsync(OperationGraph graph, IReadOnlyList<string> sources)
        {
            var maxSourceIndex = graph.Nodes.Max(x => x.Operation.SourceIndex);
            if (maxSourceIndex >= sources.Count)
            {
                throw new ArgumentException($"The max source index in the operation graph is {maxSourceIndex} so at least {maxSourceIndex + 1} sources are required.");
            }

            var operationToRequest = await RequestBuilder.BuildAsync(sources, graph.Nodes.Select(x => x.Operation));

            // Initialize all of the request nodes.
            var requestNodes = new List<RequestNode>();
            var operationNodeToRequestNode = new Dictionary<OperationNode, RequestNode>();
            foreach (var operationNode in graph.Nodes)
            {
                var requestNode = new RequestNode(
                    operationNode.HitIndex,
                    operationToRequest[operationNode.Operation]);

                requestNodes.Add(requestNode);
                operationNodeToRequestNode.Add(operationNode, requestNode);
            }

            // Initialize dependencies.
            foreach (var operationNode in graph.Nodes)
            {
                var requestNode = operationNodeToRequestNode[operationNode];
                foreach (var dependency in operationNode.Dependencies)
                {
                    requestNode.Dependencies.Add(operationNodeToRequestNode[dependency]);
                }
            }

            return new RequestGraph(requestNodes, sources.ToList());
        }

        public static async Task<OperationGraph> ToOperationGraphAsync(RequestGraph graph, IReadOnlyList<string> sources)
        {
            // Parse the request graph nodes.
            var uniqueRequests = graph.Nodes.Select(x => x.StartRequest).Distinct();
            var parsedOperations = await OperationParser.ParseAsync(sources, uniqueRequests);

            var unknownOperations = parsedOperations
                .Where(x => x.Operation == null)
                .OrderBy(x => x.Request.Method, StringComparer.Ordinal)
                .ThenBy(x => x.Request.Url, StringComparer.Ordinal)
                .ToList();
            if (unknownOperations.Any())
            {
                var builder = new StringBuilder();
                builder.AppendLine("Ensure the provided package sources are correct.");
                builder.AppendFormat("There are {0} unknown operations:", unknownOperations.Count);
                const int take = 10;
                foreach (var operation in unknownOperations.Take(take))
                {
                    builder.AppendLine();
                    builder.AppendFormat("- {0} {1}", operation.Request.Method, operation.Request.Url);
                }

                if (unknownOperations.Count > take)
                {
                    builder.AppendLine();
                    builder.AppendFormat("... and {0} others.", unknownOperations.Count - take);
                }

                throw new ArgumentException(builder.ToString());
            }

            // Initialize all of the NuGet operation nodes.
            var requestToParsedOperation = parsedOperations.ToDictionary(x => x.Request, x => x.Operation);
            var operationNodes = new List<OperationNode>();
            var requestNodeToOperationNode = new Dictionary<RequestNode, OperationNode>();
            foreach (var requestNode in graph.Nodes)
            {
                var operationNode = new OperationNode(
                    requestNode.HitIndex,
                    requestToParsedOperation[requestNode.StartRequest]);

                operationNodes.Add(operationNode);
                requestNodeToOperationNode.Add(requestNode, operationNode);
            }

            // Initialize dependencies.
            foreach (var requestNode in graph.Nodes)
            {
                var operationNode = requestNodeToOperationNode[requestNode];
                foreach (var dependency in requestNode.Dependencies)
                {
                    operationNode.Dependencies.Add(requestNodeToOperationNode[dependency]);
                }
            }

            return new OperationGraph(operationNodes);
        }
    }
}
