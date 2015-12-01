namespace Structures
{
    public class Value
    {
        public enum ValueType { String, Int, Bool, Double, Name };

        public Value(string x)
        {
            Data = x;
            Type = ValueType.String;
        }
        public Value(int x)
        {
            Data = x;
            Type = ValueType.Int;
        }
        public Value(bool x)
        {
            Data = x;
            Type = ValueType.Bool;
        }
        public Value(double x)
        {
            Data = x;
            Type = ValueType.Double;
        }
        public Value(Name x)
        {
            Data = x;
            Type = ValueType.Name;
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
