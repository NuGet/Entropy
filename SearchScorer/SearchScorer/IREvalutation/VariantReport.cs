using SearchScorer.Common;

namespace SearchScorer.IREvalutation
{
    public class VariantReport
    {
        public VariantReport(
            double score,
            SearchQueriesReport<CuratedSearchQuery> curatedSearchQueries,
            SearchQueriesReport<FeedbackSearchQuery> feedbackSearchQueries,
            SearchQueriesReport<SearchQueryWithSelections> searchQueriesWithSelections)
        {
            Score = score;
            CuratedSearchQueries = curatedSearchQueries;
            FeedbackSearchQueries = feedbackSearchQueries;
            SearchQueriesWithSelections = searchQueriesWithSelections;
        }

        public double Score { get; }
        public SearchQueriesReport<CuratedSearchQuery> CuratedSearchQueries { get; }
        public SearchQueriesReport<FeedbackSearchQuery> FeedbackSearchQueries { get; }
        public SearchQueriesReport<SearchQueryWithSelections> SearchQueriesWithSelections { get; }
    }
}
