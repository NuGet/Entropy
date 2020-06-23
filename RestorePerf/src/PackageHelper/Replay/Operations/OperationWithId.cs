using System;
using System.Diagnostics;

namespace PackageHelper.Replay.Operations
{
    [DebuggerDisplay("Source {SourceIndex}: {Type,nq} (Id: {Id,nq})")]
    public class OperationWithId : Operation, IEquatable<OperationWithId>
    {
        public OperationWithId(int sourceIndex, OperationType type, string id)
            : base(sourceIndex, type)
        {
            Id = id;
        }

        public string Id { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as OperationWithId);
        }

        public bool Equals(OperationWithId other)
        {
            return other != null &&
                   SourceIndex == other.SourceIndex &&
                   Type == other.Type &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
#if NETCOREAPP
            return HashCode.Combine(SourceIndex, Type, Id);
#else
            var hashCode = 17;
            hashCode = hashCode * 31 + SourceIndex.GetHashCode();
            hashCode = hashCode * 31 + Type.GetHashCode();
            hashCode = hashCode * 31 + Id.GetHashCode();
            return hashCode;
#endif
        }
    }
}
