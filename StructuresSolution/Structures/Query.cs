using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Structures
{
    public class Query
    {
        List<Tuple<QueryName, QueryName, QueryValue>> _query;

        public Query()
        {
            _query = new List<Tuple<QueryName, QueryName, QueryValue>>();
        }

        public void Add(QueryName s, QueryName p, QueryValue o)
        {
            _query.Add(Tuple.Create(s, p, o));
        }

        public IGraph Execute(IGraph graph)
        {
            IGraph result = new Graph();

            foreach (var tuple in _query)
            {

            }

            return result;
        }

        static Clause MakeClause()
        {
        }
    }
}
