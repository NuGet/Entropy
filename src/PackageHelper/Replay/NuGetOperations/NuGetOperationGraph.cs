using System.Collections.Generic;

namespace PackageHelper.Replay.NuGetOperations
{
    class NuGetOperationGraph
    {
        public NuGetOperationGraph(List<NuGetOperationNode> nodes)
        {
            Nodes = nodes;
        }

        public List<NuGetOperationNode> Nodes { get; }
    }
}
