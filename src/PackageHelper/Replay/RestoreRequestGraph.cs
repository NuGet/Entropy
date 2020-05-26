using PackageHelper.Replay.Requests;

namespace PackageHelper.Replay
{
    class RestoreRequestGraph
    {
        public RestoreRequestGraph(string variantName, string solutionName, RequestGraph graph)
        {
            VariantName = variantName;
            SolutionName = solutionName;
            Graph = graph;
        }

        public string VariantName { get; }
        public string SolutionName { get; }
        public RequestGraph Graph { get; }
    }
}
