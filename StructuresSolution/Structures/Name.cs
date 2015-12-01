namespace Structures
{
    public class Name
    {
        //TODO: presumably we can replace this with the System.Xml.Linq Name and Namespace
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
