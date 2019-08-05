using System.Collections.Generic;
using SearchScorer.Common;

namespace SearchScorer.IREvalutation
{
    public class VariantReport
    {
        public VariantReport(
            double score,
            double feedbackSearchQueriesScore,
            double searchQueriesWithSelectionsScore,
            IReadOnlyList<WeightedRelevancyScoreResult<FeedbackSearchQuery>> feedbackSearchQueries,
            IReadOnlyList<WeightedRelevancyScoreResult<SearchQueryWithSelections>> searchQueriesWithSelections)
        {
            Score = score;
            FeedbackSearchQueriesScore = feedbackSearchQueriesScore;
            SearchQueriesWithSelectionsScore = searchQueriesWithSelectionsScore;
            FeedbackSearchQueries = feedbackSearchQueries;
            SearchQueriesWithSelections = searchQueriesWithSelections;
        }

        public double Score { get; }
        public double FeedbackSearchQueriesScore { get; }
        public double SearchQueriesWithSelectionsScore { get; }
        public IReadOnlyList<WeightedRelevancyScoreResult<FeedbackSearchQuery>> FeedbackSearchQueries { get; }
        public IReadOnlyList<WeightedRelevancyScoreResult<SearchQueryWithSelections>> SearchQueriesWithSelections { get; }
    }
}
