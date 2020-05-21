using System.Collections.Generic;

namespace PackageHelper.RestoreReplay
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
