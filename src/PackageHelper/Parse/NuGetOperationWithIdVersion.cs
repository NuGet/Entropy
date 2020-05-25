using System;
using System.Diagnostics;

namespace PackageHelper.Parse
{
    [DebuggerDisplay("{Type,nq} (Id: {Id,nq}, Version: {Version,nq})")]
    public class NuGetOperationWithIdVersion : NuGetOperationWithId, IEquatable<NuGetOperationWithIdVersion>
    {
        public NuGetOperationWithIdVersion(NuGetOperationType type, string id, string version)
            : base(type, id)
        {
            Version = version;
        }

        public string Version { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as NuGetOperationWithIdVersion);
        }

        public bool Equals(NuGetOperationWithIdVersion other)
        {
            return other != null &&
                   Type == other.Type &&
                   Id == other.Id &&
                   Version == other.Version;
        }

        public override int GetHashCode()
        {
#if NETCOREAPP
            return HashCode.Combine(Type, Id, Version);
#else
            var hashCode = 17;
            hashCode = hashCode * 31 + Type.GetHashCode();
            hashCode = hashCode * 31 + Id.GetHashCode();
            hashCode = hashCode * 31 + Version.GetHashCode();
            return hashCode;
#endif
        }
    }
}
