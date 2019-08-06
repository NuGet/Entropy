using System.Collections.Generic;
using SearchScorer.Feedback;

namespace SearchScorer.Common
{
    public class CuratedSearchQuery
    {
        public CuratedSearchQuery(SearchQuerySource source, string searchQuery, IReadOnlyDictionary<string, int> packageIdToScore)
        {
            Source = source;
            SearchQuery = searchQuery;
            PackageIdToScore = packageIdToScore;
        }

        public SearchQuerySource Source { get; }
        public string SearchQuery { get; }
        public IReadOnlyDictionary<string, int> PackageIdToScore { get; }
    }
}
