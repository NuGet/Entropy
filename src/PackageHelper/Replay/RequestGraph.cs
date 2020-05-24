using System.Collections.Generic;

namespace PackageHelper.Replay
{
    class RequestGraph
    {
        public RequestGraph(List<RequestNode> nodes)
        {
            Nodes = nodes;
        }

        public List<RequestNode> Nodes { get; }
    }
}
