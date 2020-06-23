using System.Collections.Generic;

namespace PackageHelper.Replay.Operations
{
    class OperationGraph : IGraph<OperationNode>
    {
        public const string Type = "operationGraph";

        public OperationGraph() : this(new List<OperationNode>())
        {
        }

        public OperationGraph(List<OperationNode> nodes)
        {
            Nodes = nodes;
        }

        public List<OperationNode> Nodes { get; }
    }
}
