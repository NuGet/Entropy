using System;
using System.Collections.Generic;

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

        // http://www.gise.cse.iitb.ac.in/wiki/images/3/3a/PTSW.pdf

        public IGraph Execute(IGraph graph)
        {
            IGraph result = new Graph();

            List<IDictionary<string, Value>> bindings = null;

            foreach (var tuple in _query)
            {
                var rows = graph.Match(MakeClause(tuple));

                if (bindings == null)
                {
                    bindings = new List<IDictionary<string, Value>>();

                    foreach (var row in rows)
                    {
                        bindings.Add(MakeBinding(tuple, row));
                    }
                }
                else
                {
                    var newBindings = new List<IDictionary<string, Value>>();

                    foreach (var currentBinding in bindings)
                    {
                        foreach (var row in rows)
                        {
                            var newBinding = MakeBinding(tuple, row, currentBinding);

                            if (newBinding != null)
                            {
                                newBindings.Add(newBinding);
                            }
                        }
                    }

                    bindings = newBindings;
                }
            }

            return result;
        }

        static Clause MakeClause(Tuple<QueryName, QueryName, QueryValue> tuple)
        {
            return new Clause
            {
                Subject = tuple.Item1.Name ?? null,
                Predicate = tuple.Item2.Name ?? null,
                Object = tuple.Item3.Value ?? null
            };
        }

        static IDictionary<string, Value> MakeBinding(Tuple<QueryName, QueryName, QueryValue> tuple, Clause clause)
        {
            var result = new Dictionary<string, Value>();
            if (tuple.Item1.Variable != null)
            {
                result[tuple.Item1.Variable] = new Value(clause.Subject);
            }
            if (tuple.Item2.Variable != null)
            {
                result[tuple.Item2.Variable] = new Value(clause.Predicate);
            }
            if (tuple.Item3.Variable != null)
            {
                result[tuple.Item3.Variable] = clause.Object;
            }
            return result;
        }

        static IDictionary<string, Value> MakeBinding(Tuple<QueryName, QueryName, QueryValue> tuple, Clause clause, IDictionary<string, Value> currentBinding)
        {
            var result = new Dictionary<string, Value>(currentBinding);
            if (tuple.Item1.Variable != null)
            {
                var newValue = new Value(clause.Subject);
                if (result.ContainsKey(tuple.Item1.Variable))
                {
                    if (!result[tuple.Item1.Variable].Equals(newValue))
                    {
                        return null;
                    }
                }
                else
                {
                    result[tuple.Item1.Variable] = newValue;
                }
            }
            if (tuple.Item2.Variable != null)
            {
                var newValue = new Value(clause.Predicate);
                if (result.ContainsKey(tuple.Item2.Variable))
                {
                    if (!result[tuple.Item2.Variable].Equals(newValue))
                    {
                        return null;
                    }
                }
                else
                {
                    result[tuple.Item2.Variable] = newValue;
                }
            }
            if (tuple.Item3.Variable != null)
            {
                var newValue = clause.Object;
                if (result.ContainsKey(tuple.Item3.Variable))
                {
                    if (!result[tuple.Item3.Variable].Equals(newValue))
                    {
                        return null;
                    }
                }
                else
                {
                    result[tuple.Item3.Variable] = newValue;
                }
            }
            return result;
        }
    }
}
