using System.Collections.Generic;
using System.Diagnostics;

namespace RestoreReplay
{
    [DebuggerDisplay("{HitIndex}: {StartRequest.Url,nq}")]
    public class RequestNode
    {
        public RequestNode(int hitIndex, StartRequest startRequest, HashSet<RequestNode> dependsOn)
        {
            HitIndex = hitIndex;
            StartRequest = startRequest;
            Dependencies = new HashSet<RequestNode>(dependsOn, HitIndexAndUrlComparer.Instance);
        }

        public int HitIndex { get; }
        public StartRequest StartRequest { get; }
        public EndRequest EndRequest { get; set; }
        public HashSet<RequestNode> Dependencies { get; set; }
    }
}
