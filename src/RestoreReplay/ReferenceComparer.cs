using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RestoreReplay
{
    public class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        public static ReferenceComparer<T> Instance { get; } = new ReferenceComparer<T>();

        public bool Equals([AllowNull] T x, [AllowNull] T y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null))
            {
                return false;
            }

            if (ReferenceEquals(y, null))
            {
                return false;
            }

            return ReferenceEquals(x, y);
        }

        public int GetHashCode([DisallowNull] T obj)
        {
            return obj.GetHashCode();
        }
    }
}
