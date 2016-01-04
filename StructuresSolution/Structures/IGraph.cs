using System.Collections.Generic;

namespace Structures
{
    public interface IGraph
    {
        //void Assert(Triple facts);
        void Assert(object s, object p, object o);
        void Retract(Triple partial);
        void Add(IGraph g);
        IEnumerable<Triple> Match(Triple partial);
    }
}
