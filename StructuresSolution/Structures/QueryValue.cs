using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Structures
{
    public class QueryValue
    {
        public Value Value { get; private set; }
        public string Variable { get; private set; }

        public QueryValue(string variable)
        {
            Value = null;
            Variable = variable;
        }
        public QueryValue(Value name)
        {
            Value = name;
            Variable = null;
        }
    }
}
