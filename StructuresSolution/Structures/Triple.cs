using System.Text;

namespace Structures
{
    public class Triple
    {
        public static readonly Triple Empty;
        public object Subject { get; set; }
        public object Predicate { get; set; }
        public object Object { get; set; }

        static Triple()
        {
            Empty = new Triple();
        }

        public Triple()
        {
        }

        public Triple(object s, object p, object o)
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
