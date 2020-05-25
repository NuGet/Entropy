using System;
using System.Diagnostics;

namespace PackageHelper.Parse
{
    [DebuggerDisplay("{Type,nq}")]
    public class Operation : IEquatable<Operation>
    {
        public Operation(OperationType type)
        {
            Type = type;
        }

        public OperationType Type { get; }

        public static Operation Unknown()
        {
            return new Operation(OperationType.Unknown);
        }

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
