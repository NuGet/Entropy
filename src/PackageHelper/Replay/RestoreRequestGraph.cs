using System.Collections.Generic;
using PackageHelper.Replay.Requests;

namespace PackageHelper.Replay
{
    class RestoreRequestGraph
    {
        public RestoreRequestGraph(string variantName, string solutionName, List<string> sources, RequestGraph graph)
        {
            VariantName = variantName;
            SolutionName = solutionName;
            Sources = sources;
            Graph = graph;
        }

        public string VariantName { get; }
        public string SolutionName { get; }
        public List<string> Sources { get; }
        public RequestGraph Graph { get; }
    }
}
