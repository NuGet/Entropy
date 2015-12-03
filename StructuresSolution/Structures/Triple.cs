using System.Text;
using System.Xml.Linq;

namespace Structures
{
    public class Triple
    {
        public static readonly Triple Empty;
        public XName Subject { get; set; }
        public XName Predicate { get; set; }
        public Value Object { get; set; }

        static Triple()
        {
            Empty = new Triple();
        }

        public Triple()
        {
        }

        public Triple(XName s, XName p, Value o)
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
