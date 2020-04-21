using SearchScorer.Common;

namespace SearchScorer.IREvalutation
{
    public class VariantReport
    {
        public VariantReport(
            SearchQueriesReport<CuratedSearchQuery> curatedSearchQueries,
            SearchQueriesReport<CuratedSearchQuery> clientCuratedSearchQueries,
            SearchQueriesReport<CuratedSearchQuery> azureCuratedSearchQueries,
            SearchQueriesReport<FeedbackSearchQuery> feedbackSearchQueries)
        {
            CuratedSearchQueries = curatedSearchQueries;
            ClientCuratedSearchQueries = clientCuratedSearchQueries;
            AzureCuratedSearchQueries = azureCuratedSearchQueries;
            FeedbackSearchQueries = feedbackSearchQueries;
        }

        public SearchQueriesReport<CuratedSearchQuery> CuratedSearchQueries { get; }
        public SearchQueriesReport<CuratedSearchQuery> ClientCuratedSearchQueries { get; }
        public SearchQueriesReport<CuratedSearchQuery> AzureCuratedSearchQueries { get; }
        public SearchQueriesReport<FeedbackSearchQuery> FeedbackSearchQueries { get; }
    }
}
