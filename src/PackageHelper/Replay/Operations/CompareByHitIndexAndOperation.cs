using System;
using System.Collections.Generic;

namespace PackageHelper.Replay.Operations
{
    class CompareByHitIndexAndOperation : IEqualityComparer<OperationNode>
    {
        public static CompareByHitIndexAndOperation Instance { get; } = new CompareByHitIndexAndOperation();

        public bool Equals(OperationNode x, OperationNode y)
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

        public int GetHashCode(OperationNode obj)
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
