using System;
using System.Collections.Generic;

namespace Merge
{
    public static class SortExtensions
    {
        public static IEnumerable<T> Merge<T>(this IEnumerable<T> x, IEnumerable<T> y, IComparer<T> comparer)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (y == null)
            {
                throw new ArgumentNullException("y");
            }

            if (comparer == null)
            {
                throw new ArgumentNullException("comparer");
            }

            var ex = x.GetEnumerator();
            var ey = y.GetEnumerator();

            var fx = ex.MoveNext();
            var fy = ey.MoveNext();

            while (true)
            {
                if (fx & fy)
                {
                    if (comparer.Compare(ex.Current, ey.Current) < 0)
                    {
                        yield return ex.Current;
                        fx = ex.MoveNext();
                    }
                    else
                    {
                        yield return ey.Current;
                        fy = ey.MoveNext();
                    }
                }
                else if (fx)
                {
                    yield return ex.Current;
                    fx = ex.MoveNext();
                }
                else if (fy)
                {
                    yield return ey.Current;
                    fy = ey.MoveNext();
                }
                else
                {
                    yield break;
                }
            }
        }
    }
}
