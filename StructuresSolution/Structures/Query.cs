using System;
using System.Collections.Generic;

namespace Structures
{
    public static class Query
    {
        public static List<IDictionary<string, object>> Select(IGraph graph, IGraph query, IDictionary<string, object> parameters = null)
        {
            List<IDictionary<string, object>> bindings = null;

            foreach (var queryTriple in query.Match(Triple.Empty))
            {
                var rows = graph.Match(MakeClause(queryTriple));

                if (bindings == null)
                {
                    bindings = new List<IDictionary<string, object>>();

                    foreach (var row in rows)
                    {
                        bindings.Add(MakeBinding(queryTriple, row, parameters));
                    }
                }
                else
                {
                    var newBindings = new List<IDictionary<string, object>>();

                    foreach (var existingBinding in bindings)
                    {
                        foreach (var row in rows)
                        {
                            var newBinding = UpdateBinding(queryTriple, row, existingBinding);

                            if (newBinding != null)
                            {
                                newBindings.Add(newBinding);
                            }
                        }
                    }

                    bindings = newBindings;
                }
            }

            return bindings ?? new List<IDictionary<string, object>>();
        }

        public static IGraph Construct(IGraph graph, IGraph query, IGraph template, IDictionary<string, object> parameters = null)
        {
            IGraph result = new Graph();

            List<IDictionary<string, object>> bindings = Query.Select(graph, query, parameters);

            foreach (var binding in bindings)
            {
                foreach (var templateTriple in template.Match(Triple.Empty))
                {
                    //object s = templateTriple.Subject is Variable ? binding[((Variable)templateTriple.Subject).Value] : templateTriple.Subject;
                    //object p = templateTriple.Predicate is Variable ? binding[((Variable)templateTriple.Predicate).Value] : templateTriple.Predicate;
                    //object o = templateTriple.Object is Variable ? binding[((Variable)templateTriple.Object).Value] : templateTriple.Object;

                    object s = Construct(binding, templateTriple.Subject);
                    object p = Construct(binding, templateTriple.Predicate);
                    object o = Construct(binding, templateTriple.Object);

                    result.Assert(s, p, o);
                }
            }

            return result;
        }

        static object Construct(IDictionary<string, object> binding, object part)
        {
            if (part is Variable)
            {
                object obj = binding[((Variable)part).Value];

                if (obj is Func<IDictionary<string, object>, object>)
                {
                    return ((Func<IDictionary<string, object>, object>)obj)(binding);
                }
                else
                {
                    return obj;
                }
            }
            else
            {
                return part;
            }
        }

        static Triple MakeClause(Triple queryTriple)
        {
            return new Triple
            {
                Subject = queryTriple.Subject is Variable ? null : queryTriple.Subject,
                Predicate = queryTriple.Predicate is Variable ? null : queryTriple.Predicate,
                Object = queryTriple.Object is Variable ? null : queryTriple.Object
            };
        }

        static IDictionary<string, object> MakeBinding(Triple queryTriple, Triple row, IDictionary<string, object> parameters)
        {
            var result = new Dictionary<string, object>(parameters ?? new Dictionary<string, object>());
            if (queryTriple.Subject is Variable)
            {
                result[((Variable)queryTriple.Subject).Value] = row.Subject;
            }
            if (queryTriple.Predicate is Variable)
            {
                result[((Variable)queryTriple.Predicate).Value] = row.Predicate;
            }
            if (queryTriple.Object is Variable)
            {
                result[((Variable)queryTriple.Object).Value] = row.Object;
            }
            return result;
        }

        static IDictionary<string, object> UpdateBinding(Triple queryTriple, Triple row, IDictionary<string, object> existingBinding)
        {
            var result = new Dictionary<string, object>(existingBinding);
            if (queryTriple.Subject is Variable)
            {
                var variableName = ((Variable)queryTriple.Subject).Value;
                var newValue = row.Subject;
                if (result.ContainsKey(variableName))
                {
                    if (!result[variableName].Equals(newValue))
                    {
                        return null;
                    }
                }
                else
                {
                    result[variableName] = newValue;
                }
            }
            if (queryTriple.Predicate is Variable)
            {
                var variableName = ((Variable)queryTriple.Predicate).Value;
                var newValue = row.Predicate;
                if (result.ContainsKey(variableName))
                {
                    if (!result[variableName].Equals(newValue))
                    {
                        return null;
                    }
                }
                else
                {
                    result[variableName] = newValue;
                }
            }
            if (queryTriple.Object is Variable)
            {
                var variableName = ((Variable)queryTriple.Object).Value;
                var newValue = row.Object;
                if (result.ContainsKey(variableName))
                {
                    if (!result[variableName].Equals(newValue))
                    {
                        return null;
                    }
                }
                else
                {
                    result[variableName] = newValue;
                }
            }
            return result;
        }
    }
}
