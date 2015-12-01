namespace Structures
{
    public class Name
    {
        public Name(string data)
        {
            Data = data;
        }

        public string Data { get; set; }

        public override string ToString()
        {
            return Data;
        }

        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Data.Equals(((Name)obj).Data);
        }
    }
}
