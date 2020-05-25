using System.Collections.Generic;
using System.Diagnostics;
using PackageHelper.Parse;

namespace PackageHelper.Replay.Operations
{
    [DebuggerDisplay("{HitIndex}: {Operation,nq}")]
    class OperationNode
    {
        public OperationNode(int hitIndex, Operation operation, HashSet<OperationNode> dependencies)
        {
            HitIndex = hitIndex;
            Operation = operation;
            Dependencies = new HashSet<OperationNode>(dependencies, CompareByHitIndexAndOperation.Instance);
        }

        public int HitIndex { get; }
        public Operation Operation { get; }
        public HashSet<OperationNode> Dependencies { get; set; }
    }
}
