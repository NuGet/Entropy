using System.Collections.Generic;
using System.Diagnostics;
using PackageHelper.Parse;

namespace PackageHelper.Replay.NuGetOperations
{
    [DebuggerDisplay("{HitIndex}: {Operation,nq}")]
    class NuGetOperationNode
    {
        public NuGetOperationNode(int hitIndex, NuGetOperation operation, HashSet<NuGetOperationNode> dependencies)
        {
            HitIndex = hitIndex;
            Operation = operation;
            Dependencies = new HashSet<NuGetOperationNode>(dependencies, CompareByHitIndexAndOperation.Instance);
        }

        public int HitIndex { get; }
        public NuGetOperation Operation { get; }
        public HashSet<NuGetOperationNode> Dependencies { get; set; }
    }
}
