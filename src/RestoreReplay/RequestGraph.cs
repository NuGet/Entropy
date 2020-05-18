using System.Collections.Generic;

namespace RestoreReplay
{
    public class RequestGraph
    {
        public RequestGraph(List<RequestNode> nodes, List<string> sources, int maxConcurrency)
        {
            Nodes = nodes;
            Sources = sources;
            MaxConcurrency = maxConcurrency;
        }

        public List<RequestNode> Nodes { get; }
        public List<string> Sources { get; }
        public int MaxConcurrency { get; set; }
    }
}
