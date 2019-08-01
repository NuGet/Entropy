using System.Collections.Generic;

namespace SearchScorer.Feedback
{
    public class VariantResult
    {
        public VariantResult(
            int? resultIndex,
            ResultIndexBucket resultIndexBucket,
            int? pageIndex,
            IReadOnlyList<string> foundIds,
            SearchResponse response)
        {
            ResultIndex = resultIndex;
            ResultIndexBucket = resultIndexBucket;
            PageIndex = pageIndex;
            FoundIds = foundIds;
            Response = response;
        }

        public int? ResultIndex { get; }
        public ResultIndexBucket ResultIndexBucket { get; }
        public int? PageIndex { get; }
        public IReadOnlyList<string> FoundIds { get; }
        public SearchResponse Response { get; }
    }
}
