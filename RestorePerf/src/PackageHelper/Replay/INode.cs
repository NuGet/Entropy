using System.Collections.Generic;

namespace PackageHelper.Replay
{
    interface INode<TDependency>
    {
        HashSet<TDependency> Dependencies { get; }
    }
}
