using System;
using System.Collections.Generic;

namespace PackageHelper.Replay.NuGetOperations
{
    class CompareByHitIndexAndOperation : IEqualityComparer<NuGetOperationNode>
    {
        public static CompareByHitIndexAndOperation Instance { get; } = new CompareByHitIndexAndOperation();

        public bool Equals(NuGetOperationNode x, NuGetOperationNode y)
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
                && x.Operation.Equals(y.Operation);
        }

        public int GetHashCode(NuGetOperationNode obj)
        {
#if NETCOREAPP
            return HashCode.Combine(obj.HitIndex, obj.Operation);
#else
            var hashCode = 17;
            hashCode = hashCode * 31 + obj.HitIndex.GetHashCode();
            hashCode = hashCode * 31 + obj.Operation.GetHashCode();
            return hashCode;
#endif
        }
    }
}
