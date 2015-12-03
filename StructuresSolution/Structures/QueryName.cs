using System.Xml.Linq;

namespace Structures
{
    public class QueryName
    {
        public XName Name { get; private set; }
        public string Variable { get; private set; }

        public QueryName(object obj)
        {
            if (obj is XName)
            {
                Name = (XName)obj;
                Variable = null;
            }
            else
            {
                Name = null;
                Variable = ((Variable)obj).Value;
            }
        }
    }
}
