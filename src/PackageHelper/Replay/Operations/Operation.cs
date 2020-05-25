using System;
using System.Diagnostics;

namespace PackageHelper.Replay.Operations
{
    [DebuggerDisplay("{Type,nq}")]
    public class Operation : IEquatable<Operation>
    {
        public Operation(OperationType type)
        {
            Type = type;
        }

        public OperationType Type { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Operation);
        }

        public bool Equals(Operation other)
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
