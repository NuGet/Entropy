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
    }
}
