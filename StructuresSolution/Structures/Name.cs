using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Structures
{
    public class Name
    {
        public Name(string name)
        {
            Value = name;
        }
        public string Value { get; private set; }
        public override string ToString()
        {
            return string.Format("<{0}>", Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return Value.Equals(((Name)obj).Value);
        }
    }
}
