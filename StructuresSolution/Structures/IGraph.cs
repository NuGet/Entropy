using System.Collections.Generic;

namespace Structures
{
    public interface IGraph
    {
        void Assert(Clause facts);
        void Retract(Clause partial);
        void Add(IGraph g);
        IEnumerable<Clause> Match(Clause partial);
    }
}
