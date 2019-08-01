using System;
using System.Collections.Generic;
using System.Linq;

namespace SearchScorer.Feedback
{
    public class FeedbackResultAggregator
    {
        private readonly List<FeedbackResult> _results = new List<FeedbackResult>();

        private IEnumerable<FeedbackResult> SortedResults => _results
            .OrderBy(x => x.FeedbackItem.Query, StringComparer.OrdinalIgnoreCase);

        public void Add(FeedbackResult result)
        {
            _results.Add(result);
        }

        public int GetMaxQueryLength() => _results.Max(x => x.FeedbackItem.Query.Length);

        public List<FeedbackResult> GetResultsThatDroppedOffTheFirstPage()
        {
            return GetResultsWithBucketTransition(
                ResultIndexBucket.AboveFold,
                ResultIndexBucket.NotInFirstPage);
        }

        public List<FeedbackResult> GetResultsThatDroppedBelowTheFold()
        {
            return GetResultsWithBucketTransition(
                ResultIndexBucket.AboveFold,
                ResultIndexBucket.BelowFold);
        }

        public List<FeedbackResult> GetResultsThatRoseToBelowTheFold()
        {
            return GetResultsWithBucketTransition(
                ResultIndexBucket.NotInFirstPage,
                ResultIndexBucket.BelowFold);
        }

        public List<FeedbackResult> GetResultsThatMovedUpAboveTheFold()
        {
            return GetResultsWithIndexChangeWithinTheSameBucket(
                ResultIndexBucket.AboveFold,
                (c, t) => c > t);
        }

        public List<FeedbackResult> GetResultsThatMovedDownAboveTheFold()
        {
            return GetResultsWithIndexChangeWithinTheSameBucket(
                ResultIndexBucket.AboveFold,
                (c, t) => c < t);
        }

        public List<FeedbackResult> GetResultsThatMovedUpBelowTheFold()
        {
            return GetResultsWithIndexChangeWithinTheSameBucket(
                ResultIndexBucket.BelowFold,
                (c, t) => c > t);
        }

        public List<FeedbackResult> GetResultsThatMovedDownBelowTheFold()
        {
            return GetResultsWithIndexChangeWithinTheSameBucket(
                ResultIndexBucket.BelowFold,
                (c, t) => c < t);
        }

        private List<FeedbackResult> GetResultsWithIndexChangeWithinTheSameBucket(
            ResultIndexBucket bucket,
            Func<int, int, bool> include)
        {
            var output = new List<FeedbackResult>();

            foreach (var result in SortedResults)
            {
                if (result.ControlResult.ResultIndexBucket == bucket
                    && result.TreatmentResult.ResultIndexBucket == bucket
                    && result.ControlResult.ResultIndex.HasValue
                    && result.TreatmentResult.ResultIndex.HasValue
                    && include(result.ControlResult.ResultIndex.Value, result.TreatmentResult.ResultIndex.Value))
                {
                    output.Add(result);
                }
            }

            return output;
        }

        private List<FeedbackResult> GetResultsWithBucketTransition(ResultIndexBucket controlBucket, ResultIndexBucket treatmentBucket)
        {
            var output = new List<FeedbackResult>();

            foreach (var result in SortedResults)
            {
                if (result.ControlResult.ResultIndexBucket == controlBucket
                    && result.TreatmentResult.ResultIndexBucket == treatmentBucket)
                {
                    output.Add(result);
                }
            }

            return output;
        }

        public SortedDictionary<FeedbackResultType, List<FeedbackResult>> GetResultTypeToResults()
        {
            var output = new SortedDictionary<FeedbackResultType, List<FeedbackResult>>();

            foreach (var result in SortedResults)
            {
                if (!output.TryGetValue(result.Type, out var results))
                {
                    results = new List<FeedbackResult>();
                    output.Add(result.Type, results);
                }

                results.Add(result);
            }

            return output;
        }
    }
}
