using System;
using System.Diagnostics;

namespace PackageHelper.Replay.Operations
{
    [DebuggerDisplay("Source {SourceIndex}: {Type,nq}")]
    public class Operation : IEquatable<Operation>
    {
        public Operation(int sourceIndex, OperationType type)
        {
            SourceIndex = sourceIndex;
            Type = type;
        }

        public int SourceIndex { get; }
        public OperationType Type { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Operation);
        }

        public bool Equals(Operation other)
        {
            return other != null &&
                   SourceIndex == other.SourceIndex &&
                   Type == other.Type;
        }

        public override int GetHashCode()
        {
#if NETCOREAPP
            return HashCode.Combine(SourceIndex, Type);
#else
            var hashCode = 17;
            hashCode = hashCode * 31 + SourceIndex.GetHashCode();
            hashCode = hashCode * 31 + Type.GetHashCode();
            return hashCode;
#endif
        }
    }
}
