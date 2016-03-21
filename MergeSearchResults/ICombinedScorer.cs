using System.Collections.Generic;

namespace MergeSearchResults
{
    public interface ICombinedScorer<T>
    {
        IEnumerable<T> DedupAndAdd(IEnumerable<T> searchResult);

        IComparer<T> CreateComparer();
    }
}
