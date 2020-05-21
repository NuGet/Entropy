using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PackageHelper.RestoreReplay
{
    class HitIndexAndUrlComparer : IEqualityComparer<RequestNode>
    {
        public static HitIndexAndUrlComparer Instance { get; } = new HitIndexAndUrlComparer();

        public bool Equals([AllowNull] RequestNode x, [AllowNull] RequestNode y)
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

            return x.HitIndex == y.HitIndex
                && x.StartRequest.Url == y.StartRequest.Url;
        }

        public int GetHashCode([DisallowNull] RequestNode obj)
        {
            return HashCode.Combine(obj.HitIndex, obj.StartRequest.Url);
        }
    }
}
