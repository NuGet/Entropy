using System.Text;

namespace Structures
{
    public class Clause
    {
        public static readonly Clause Empty;
        public Name Subject { get; set; }
        public Name Predicate { get; set; }
        public Value Object { get; set; }

        static Clause()
        {
            Empty = new Clause();
        }

        public Clause()
        {
        }

        public Clause(Name s, Name p, Value o)
        {
            Subject = s;
            Predicate = p;
            Object = o;
        }

        public string Display()
        {
            var sb = new StringBuilder();
            if (Subject != null)
            {
                sb.AppendFormat(" Subject: {0}", Subject);
            }
            if (Predicate != null)
            {
                sb.AppendFormat(" Predicate: {0}", Predicate);
            }
            if (Object != null)
            {
                sb.AppendFormat(" Object: {0}", Object);
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} .", Subject, Predicate, Object);
        }
    }
}
