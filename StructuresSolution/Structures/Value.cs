using System;
using System.Xml.Linq;

namespace Structures
{
    public class Value
    {
        public enum ValueType { String, Int, Bool, Double, Name };

        public Value(object o)
        {
            Data = o;
            Type = GetType(o);
        }

        static ValueType GetType(object o)
        {
            if (o is XName)
            {
                return ValueType.Name;
            }
            if (o is string)
            {
                return ValueType.String;
            }
            if (o is int)
            {
                return ValueType.Int;
            }
            if (o is double)
            {
                return ValueType.Double;
            }
            if (o is bool)
            {
                return ValueType.Bool;
            }
            throw new ArgumentException(o.GetType().Name);
        }

        public object Data
        {
            get; set;
        }
        public ValueType Type
        {
            get; set;
        }

        public override string ToString()
        {
            return Data.ToString();
        }

        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return Data.Equals(((Value)obj).Data);
        }
    }
}
