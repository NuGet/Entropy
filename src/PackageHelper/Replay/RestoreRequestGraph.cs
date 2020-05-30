using PackageHelper.Replay.Requests;

namespace PackageHelper.Replay
{
    class RestoreRequestGraph
    {
        public RestoreRequestGraph(string fileName, string variantName, string solutionName, RequestGraph graph)
        {
            FileName = fileName;
            VariantName = variantName;
            SolutionName = solutionName;
            Graph = graph;
        }

        public string FileName { get; }
        public string VariantName { get; }
        public string SolutionName { get; }
        public RequestGraph Graph { get; }
    }
}
