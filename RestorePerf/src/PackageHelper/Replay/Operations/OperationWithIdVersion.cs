using System;
using System.Diagnostics;

namespace PackageHelper.Replay.Operations
{
    [DebuggerDisplay("Source {SourceIndex}: {Type,nq} (Id: {Id,nq}, Version: {Version,nq})")]
    public class OperationWithIdVersion : OperationWithId, IEquatable<OperationWithIdVersion>
    {
        public OperationWithIdVersion(int sourceIndex, OperationType type, string id, string version)
            : base(sourceIndex, type, id)
        {
            Version = version;
        }

        public string Version { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as OperationWithIdVersion);
        }

        public bool Equals(OperationWithIdVersion other)
        {
            return other != null &&
                   SourceIndex == other.SourceIndex &&
                   Type == other.Type &&
                   Id == other.Id &&
                   Version == other.Version;
        }

        public override int GetHashCode()
        {
#if NETCOREAPP
            return HashCode.Combine(SourceIndex, Type, Id, Version);
#else
            var hashCode = 17;
            hashCode = hashCode * 31 + SourceIndex.GetHashCode();
            hashCode = hashCode * 31 + Type.GetHashCode();
            hashCode = hashCode * 31 + Id.GetHashCode();
            hashCode = hashCode * 31 + Version.GetHashCode();
            return hashCode;
#endif
        }
    }
}
