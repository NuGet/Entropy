namespace Structures
{
    public class Entry
    {
        public Name Subject { get; set; }
        public Name Predicate { get; set; }
        public Value Object { get; set; }

        public Entry()
        {
        }

        public Entry(Name s, Name p, Value o)
        {
            Subject = s;
            Predicate = p;
            Object = o;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} .", Subject, Predicate, Object);
        }
    }
}
