using SearchScorer.Common;

namespace SearchScorer.IREvalutation
{
    public class VariantReport
    {
        public VariantReport(
            SearchQueriesReport<CuratedSearchQuery> curatedSearchQueries,
            SearchQueriesReport<FeedbackSearchQuery> feedbackSearchQueries)
        {
            CuratedSearchQueries = curatedSearchQueries;
            FeedbackSearchQueries = feedbackSearchQueries;
        }

        public SearchQueriesReport<CuratedSearchQuery> CuratedSearchQueries { get; }
        public SearchQueriesReport<FeedbackSearchQuery> FeedbackSearchQueries { get; }
    }
}
