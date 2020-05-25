using System.Collections.Generic;

namespace PackageHelper.Replay
{
    interface IGraph<TNode> where TNode : INode<TNode>
    {
        List<TNode> Nodes { get; }
    }
}
