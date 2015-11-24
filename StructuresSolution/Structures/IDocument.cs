using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Structures
{
    public interface IDocument
    {
        void Assert(IEnumerable<Entry> facts);
        void Retract(IEnumerable<Entry> partial);
        IEnumerable<Entry> Match(IEnumerable<Entry> partial);
    }
}
