using System.Collections.Generic;
using System.Diagnostics;

namespace PackageHelper.Replay.Requests
{
    [DebuggerDisplay("{HitIndex}: {StartRequest,nq}")]
    class RequestNode : INode<RequestNode>
    {
        public RequestNode(int hitIndex, StartRequest startRequest)
            : this(hitIndex, startRequest, new HashSet<RequestNode>())
        {
        }

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
