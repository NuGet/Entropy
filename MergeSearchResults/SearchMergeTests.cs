using Merge;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MergeSearchResults
{
    public class SearchMergeTests
    {
        public void Test0()
        {
            var results = new[]
            {
                CreateSearchResult0(),
                CreateSearchResult1(),
                CreateSearchResult2()
            };

            // create the scorer, add all the results we have - dedupping as we go, and create a comparer

            var scorer = new LuceneScorer();

            var resultsToMerge = new List<IEnumerable<PackageSearchResult>>();

            foreach (var result in results)
            {
                resultsToMerge.Add(scorer.DedupAndAdd(result));
            }

            var comparer = scorer.CreateComparer();

            // then use that comparer to merge the (now depupped) results

            var acc = Enumerable.Empty<PackageSearchResult>();
            foreach (var result in resultsToMerge)
            {
                acc = acc.Merge(result, comparer);
            }

            foreach (var i in acc)
            {
                Console.Write($"{i} ");
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------");
        }

        public void Test1()
        {
            var resultsPhase1 = new[]
            {
                CreateSearchResult0(),
                CreateSearchResult1(),
            };

            // create the scorer, add all the results we have - dedupping as we go, and create a comparer

            var scorer = new LuceneScorer();

            var resultsToMerge = new List<IEnumerable<PackageSearchResult>>();

            foreach (var result in resultsPhase1)
            {
                resultsToMerge.Add(scorer.DedupAndAdd(result));
            }

            var comparerPhase1 = scorer.CreateComparer();

            // then use that comparer to merge the (now depupped) results

            var acc = Enumerable.Empty<PackageSearchResult>();
            foreach (var result in resultsToMerge)
            {
                acc = acc.Merge(result, comparerPhase1);
            }

            // a second set of results arrives...

            var resultsPhase2 = new[]
            {
                CreateSearchResult2(),
                CreateSearchResult3(),
            };

            foreach (var result in resultsPhase2)
            {
                resultsToMerge.Add(scorer.DedupAndAdd(result));
            }

            var comparerPhase2 = scorer.CreateComparer();

            // then use that comparer to merge the (now depupped) results - here we start over on the merge because we have a new comparer

            acc = Enumerable.Empty<PackageSearchResult>();

            foreach (var result in resultsToMerge)
            {
                acc = acc.Merge(result, comparerPhase2);
            }

            foreach (var i in acc)
            {
                Console.Write($"{i} ");
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------");
        }

        private IEnumerable<PackageSearchResult> CreateSearchResult0()
        {
            return Enumerable.Empty<PackageSearchResult>();
        }

        private IEnumerable<PackageSearchResult> CreateSearchResult1()
        {
            return Enumerable.Empty<PackageSearchResult>();
        }
        private IEnumerable<PackageSearchResult> CreateSearchResult2()
        {
            return Enumerable.Empty<PackageSearchResult>();
        }
        private IEnumerable<PackageSearchResult> CreateSearchResult3()
        {
            return Enumerable.Empty<PackageSearchResult>();
        }
    }
}
