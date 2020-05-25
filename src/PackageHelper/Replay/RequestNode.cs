using System.Collections.Generic;
using System.Diagnostics;

namespace PackageHelper.Replay
{
    [DebuggerDisplay("{HitIndex}: {StartRequest,nq}")]
    class RequestNode
    {
        public RequestNode(int hitIndex, StartRequest startRequest, HashSet<RequestNode> dependencies)
        {
            HitIndex = hitIndex;
            StartRequest = startRequest;
            Dependencies = new HashSet<RequestNode>(dependencies, CompareByHitIndexAndRequest.Instance);
        }

        public int HitIndex { get; }
        public StartRequest StartRequest { get; }
        public EndRequest EndRequest { get; set; }
        public HashSet<RequestNode> Dependencies { get; set; }
    }
}
