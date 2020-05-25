using System.Collections.Generic;

namespace PackageHelper.Replay.Requests
{
    class RequestGraph : IGraph<RequestNode>
    {
        public const string Type = "requestGraph";

        public RequestGraph() : this(new List<RequestNode>())
        {
        }

        public RequestGraph(List<RequestNode> nodes)
        {
            Nodes = nodes;
        }

        public List<RequestNode> Nodes { get; }
    }
}
