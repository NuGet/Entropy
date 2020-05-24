using System;
using System.Collections.Generic;

namespace PackageHelper.RestoreReplay
{
    class HitIndexAndUrlComparer : IEqualityComparer<RequestNode>
    {
        public static HitIndexAndUrlComparer Instance { get; } = new HitIndexAndUrlComparer();

        public bool Equals(RequestNode x, RequestNode y)
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

        public int GetHashCode(RequestNode obj)
        {
#if NETCOREAPP
            return HashCode.Combine(obj.HitIndex, obj.StartRequest.Url);
#else
            var hasCode = 17;
            hasCode = hasCode * 31 + obj.HitIndex.GetHashCode();
            hasCode = hasCode * 31 + obj.StartRequest.Url.GetHashCode();
            return hasCode;
#endif
        }
    }
}
