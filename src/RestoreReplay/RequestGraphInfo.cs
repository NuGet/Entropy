namespace RestoreReplay
{
    public class RequestGraphInfo
    {
        public RequestGraphInfo(string solutionName, RequestGraph graph)
        {
            SolutionName = solutionName;
            Graph = graph;
        }

        public string SolutionName { get; }
        public RequestGraph Graph { get; }
    }
}
