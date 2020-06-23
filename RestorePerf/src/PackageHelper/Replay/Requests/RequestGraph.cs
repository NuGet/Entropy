using System.Collections.Generic;

namespace PackageHelper.Replay.Requests
{
    class RequestGraph : IGraph<RequestNode>
    {
        public const string Type = "requestGraph";

        public RequestGraph() : this(new List<RequestNode>(), new List<string>())
        {
        }

        public RequestGraph(List<RequestNode> nodes, List<string> sources)
        {
            Nodes = nodes;
            Sources = sources;
        }

        public List<string> Sources { get; }
        public List<RequestNode> Nodes { get; }
    }
}
