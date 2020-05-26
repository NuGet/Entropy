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
        public static async Task<OperationGraph> ToOperationGraphAsync(IReadOnlyList<string> sources, RequestGraph graph)
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
            var operations = new List<OperationNode>();
            var requestToOperation = new Dictionary<RequestNode, OperationNode>();
            foreach (var request in graph.Nodes)
            {
                var operation = new OperationNode(
                    request.HitIndex,
                    requestToParsedOperation[request.StartRequest]);

                operations.Add(operation);
                requestToOperation.Add(request, operation);
            }

            // Initialize dependencies.
            foreach (var request in graph.Nodes)
            {
                var operation = requestToOperation[request];
                foreach (var dependency in request.Dependencies)
                {
                    operation.Dependencies.Add(requestToOperation[dependency]);
                }
            }

            return new OperationGraph(operations);
        }
    }
}
