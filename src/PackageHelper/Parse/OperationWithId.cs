using System;
using System.Diagnostics;

namespace PackageHelper.Parse
{
    [DebuggerDisplay("{Type,nq} (Id: {Id,nq})")]
    public class OperationWithId : Operation, IEquatable<OperationWithId>
    {
        public OperationWithId(OperationType type, string id)
            : base(type)
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
                   Type == other.Type &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
#if NETCOREAPP
            return HashCode.Combine(Type, Id);
#else
            var hashCode = 17;
            hashCode = hashCode * 31 + Type.GetHashCode();
            hashCode = hashCode * 31 + Id.GetHashCode();
            return hashCode;
#endif
        }
    }
}
