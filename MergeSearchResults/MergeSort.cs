using System;
using System.Collections.Generic;
using System.Linq;

namespace Merge
{
    static class MergeSortExtensions
    {
        static IEnumerable<T> CompareAndSwap<T>(this IEnumerable<T> xl, IComparer<T> comparer)
        {
            if (!xl.Skip(1).Any())
            {
                return xl;
            }
            if (comparer.Compare(xl.First(), xl.Skip(1).First()) < 0)
            {
                return xl;
            }
            return new[] { xl.Skip(1).First(), xl.First() };
        }

        static Tuple<IEnumerable<T>, IEnumerable<T>> Split<T>(this IEnumerable<T> xl)
        {
            int n = xl.Count();
            var lhs = xl.Take(n / 2);
            var rhs = xl.Skip(n / 2).Take(n);
            return Tuple.Create(lhs, rhs);
        }

        public static IEnumerable<T> MergeSort<T>(this IEnumerable<T> xl, IComparer<T> compare)
        {
            var splits = xl.Split();
            var lhs = splits.Item1.Skip(2).Any() ? splits.Item1.MergeSort(compare) : splits.Item1.CompareAndSwap(compare);
            var rhs = splits.Item2.Skip(2).Any() ? splits.Item2.MergeSort(compare) : splits.Item2.CompareAndSwap(compare);
            return lhs.Merge(rhs, compare);
        }
    }
}
