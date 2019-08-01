using System.Collections.Generic;
using System.Linq;

namespace SearchScorer.Feedback
{
    public class FeedbackItem
    {
        public FeedbackItem(
            FeedbackSource source,
            FeedbackDisposition disposition,
            string query,
            IEnumerable<string> mostRelevantPackageIds,
            params Bucket[] buckets)
        {
            Source = source;
            Disposition = disposition;
            Query = query;
            Buckets = buckets.ToList();
            MostRelevantPackageIds = mostRelevantPackageIds.ToList();
        }

        public FeedbackSource Source { get; }
        public FeedbackDisposition Disposition { get; }
        public string Query { get; }
        public IReadOnlyList<Bucket> Buckets { get; }
        public IReadOnlyList<string> MostRelevantPackageIds { get; }
    }
}
