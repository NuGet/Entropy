namespace Structures
{
    public class Variable
    {
        public string Value { get; private set; }

        public Variable(string name)
        {
            Value = name;
        }
        public override string ToString()
        {
            return Value.ToString();
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return Value.Equals(((Variable)obj).Value);
        }
    }
}
