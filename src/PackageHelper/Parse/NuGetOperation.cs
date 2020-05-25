using System;
using System.Diagnostics;

namespace PackageHelper.Parse
{
    [DebuggerDisplay("{Type,nq}")]
    public class NuGetOperation : IEquatable<NuGetOperation>
    {
        public NuGetOperation(NuGetOperationType type)
        {
            Type = type;
        }

        public NuGetOperationType Type { get; }

        public static NuGetOperation Unknown()
        {
            return new NuGetOperation(NuGetOperationType.Unknown);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NuGetOperation);
        }

        public bool Equals(NuGetOperation other)
        {
            return other != null &&
                   Type == other.Type;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }
    }
}
