using System;
using System.Collections.Generic;

namespace PackageHelper.Replay
{
    class HitIndexAndRequestComparer : IEqualityComparer<RequestNode>
    {
        public static HitIndexAndRequestComparer Instance { get; } = new HitIndexAndRequestComparer();

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
                && x.StartRequest.Method == y.StartRequest.Method
                && x.StartRequest.Url == y.StartRequest.Url;
        }

        public int GetHashCode(RequestNode obj)
        {
#if NETCOREAPP
            return HashCode.Combine(
                obj.HitIndex,
                obj.StartRequest.Method,
                obj.StartRequest.Url);
#else
            var hashCode = 17;
            hashCode = hashCode * 31 + obj.HitIndex.GetHashCode();
            hashCode = hashCode * 31 + obj.StartRequest.Method.GetHashCode();
            hashCode = hashCode * 31 + obj.StartRequest.Url.GetHashCode();
            return hashCode;
#endif
        }
    }
}
