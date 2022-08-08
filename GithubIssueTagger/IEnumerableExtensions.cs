using System;
using System.Collections.Generic;
using System.Linq;

namespace GithubIssueTagger
{
    internal static class IEnumerableExtensions
    {
        internal static T? MinOrDefault<T>(this IEnumerable<T> enumerable) where T : IComparable
        {
            try
            {
                return enumerable.Min();
            }
            catch (InvalidOperationException)
            {
                return default;
            }
        }
    }
}
