using System.Collections.Generic;

namespace Structures
{
    public interface IGraph
    {
        void Assert(Triple facts);
        void Retract(Triple partial);
        void Add(IGraph g);
        IEnumerable<Triple> Match(Triple partial);
    }
}
