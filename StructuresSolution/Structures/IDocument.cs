using System.Collections.Generic;

namespace Structures
{
    public interface IDocument
    {
        void Assert(IEnumerable<Entry> facts);
        void Retract(IEnumerable<Entry> partial);
        IEnumerable<Entry> Match(IEnumerable<Entry> partial);
    }
}
