using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Structures
{
    public class Entry
    {
        public Name Subject { get; set; }
        public Name Predicate { get; set; }
        public Value Object { get; set; }
    }
}
