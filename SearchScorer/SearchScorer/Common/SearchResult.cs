namespace SearchScorer.Common
{
    public class SearchResult
    {
        public string Id { get; set; }
        public SearchResultDebug Debug { get; set; }
    }

    public class SearchResultDebug
    {
        public double Score { get; set; }
        public SearchResultDebugDocument Document { get; set; }
    }

    public class SearchResultDebugDocument
    {
        public long TotalDownloadCount { get; set; }
    }
}
