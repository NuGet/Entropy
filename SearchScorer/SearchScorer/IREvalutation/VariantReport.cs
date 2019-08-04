using System.Collections.Generic;
using SearchScorer.Common;

namespace SearchScorer.IREvalutation
{
    public class VariantReport
    {
        public VariantReport(
            double score,
            double testSearchQueriesScore,
            double searchQueriesWithSelectionsScore,
            IReadOnlyList<WeightedRelevancyScoreResult<TestSearchQuery>> testSearchQueries,
            IReadOnlyList<WeightedRelevancyScoreResult<SearchQueryWithSelections>> searchQueriesWithSelections)
        {
            Score = score;
            TestSearchQueriesScore = testSearchQueriesScore;
            SearchQueriesWithSelectionsScore = searchQueriesWithSelectionsScore;
            TestSearchQueries = testSearchQueries;
            SearchQueriesWithSelections = searchQueriesWithSelections;
        }

        public double Score { get; }
        public double TestSearchQueriesScore { get; }
        public double SearchQueriesWithSelectionsScore { get; }
        public IReadOnlyList<WeightedRelevancyScoreResult<TestSearchQuery>> TestSearchQueries { get; }
        public IReadOnlyList<WeightedRelevancyScoreResult<SearchQueryWithSelections>> SearchQueriesWithSelections { get; }
    }
}
