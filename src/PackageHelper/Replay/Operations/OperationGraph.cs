using System.Collections.Generic;

namespace PackageHelper.Replay.Operations
{
    class OperationGraph
    {
        public OperationGraph(List<OperationNode> nodes)
        {
            Nodes = nodes;
        }

        public List<OperationNode> Nodes { get; }
    }
}
