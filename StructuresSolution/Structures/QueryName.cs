using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Structures
{
    public class QueryName
    {
        public Name Name { get; private set; }
        public string Variable { get; private set; }

        public QueryName(string variable)
        {
            Name = null;
            Variable = variable;
        }
        public QueryName(Name name)
        {
            Name = name;
            Variable = null;
        }
    }
}
