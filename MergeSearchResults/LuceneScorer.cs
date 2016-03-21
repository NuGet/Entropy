using System.Collections.Generic;

namespace MergeSearchResults
{
    public class LuceneScorer : ICombinedScorer<PackageSearchResult>
    {
        public LuceneScorer()
        {
        }

        public IEnumerable<PackageSearchResult> DedupAndAdd(IEnumerable<PackageSearchResult> searchResult)
        {
            //TODO: add to teh accumulated index but check for duplicates as you do
            //TODO: duplicates should have their version lists merged

            return searchResult;
        }

        public IComparer<PackageSearchResult> CreateComparer()
        {
            //TODO: all the scoring calculation goes in here

            var scores = new Dictionary<PackageIdentity, double>();

            return new ScoreComparer(scores);
        }

        class ScoreComparer : Comparer<PackageSearchResult>
        {
            IDictionary<PackageIdentity, double> _scores;

            public ScoreComparer(IDictionary<PackageIdentity, double> scores)
            {
                _scores = scores;
            }

            public override int Compare(PackageSearchResult x, PackageSearchResult y)
            {
                var ix = GetIdentity(x);
                var iy = GetIdentity(y);

                double sx = 0.0;
                _scores.TryGetValue(ix, out sx);

                double sy = 0.0;
                _scores.TryGetValue(iy, out sy);

                return sx == sy ? 0 : (sx > sy ? -1 : 1);
            }

            private static PackageIdentity GetIdentity(PackageSearchResult p)
            {
                //TODO: extract the package identity

                return new PackageIdentity();
            }
        }
    }
}
