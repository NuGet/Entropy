using System;
using System.Collections.Generic;
using System.Text;

namespace Structures
{
    public class Graph : IGraph
    {
        IDictionary<Name, Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>>> _s;
        IDictionary<Name, Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>>> _p;
        IDictionary<Value, Tuple<IDictionary<Name, ISet<Name>>, IDictionary<Name, ISet<Name>>>> _o;

        public Graph()
        {
            _s = new Dictionary<Name, Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>>>();
            _p = new Dictionary<Name, Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>>>();
            _o = new Dictionary<Value, Tuple<IDictionary<Name, ISet<Name>>, IDictionary<Name, ISet<Name>>>>();
        }

        public void Assert(Clause fact)
        {
            if (fact == null || fact.Subject == null || fact.Predicate == null || fact.Object == null)
            {
                throw new ArgumentNullException("fact");
            }

            _s = AddIndex(_s, fact.Subject, fact.Predicate, fact.Object);
            _p = AddIndex(_p, fact.Predicate, fact.Subject, fact.Object);
            _o = AddIndex(_o, fact.Object, fact.Subject, fact.Predicate);
        }
        public void Retract(Clause fact)
        {
            if (fact == null || fact.Subject == null || fact.Predicate == null || fact.Object == null)
            {
                throw new ArgumentNullException("fact");
            }
        }

        public void Add(IGraph g)
        {
            foreach (var fact in g.Match(Clause.Empty))
            {
                Assert(fact);
            }
        }

        public IEnumerable<Clause> Match(Clause partial)
        {
            if (partial == null)
            {
                throw new ArgumentNullException("partial");
            }

            if (partial.Subject != null)
            {
                if (partial.Predicate != null)
                {
                    if (partial.Object != null)
                    {
                        foreach (Clause clause in GetBySubjectPredicateObject(partial.Subject, partial.Predicate, partial.Object))
                        {
                            yield return clause;
                        }
                    }
                    else
                    {
                        foreach (Clause clause in GetBySubjectPredicate(partial.Subject, partial.Predicate))
                        {
                            yield return clause;
                        }
                    }
                }
                else
                {
                    if (partial.Object != null)
                    {
                        foreach (Clause clause in GetBySubjectObject(partial.Subject, partial.Object))
                        {
                            yield return clause;
                        }
                    }
                    else
                    {
                        foreach (Clause clause in GetBySubject(partial.Subject))
                        {
                            yield return clause;
                        }
                    }
                }
            }
            else
            {
                if (partial.Predicate != null)
                {
                    if (partial.Object != null)
                    {
                        foreach (Clause clause in GetByPredicateObject(partial.Predicate, partial.Object))
                        {
                            yield return clause;
                        }
                    }
                    else
                    {
                        foreach (Clause clause in GetByPredicate(partial.Predicate))
                        {
                            yield return clause;
                        }
                    }
                }
                else
                {
                    if (partial.Object != null)
                    {
                        foreach (Clause clause in GetByObject(partial.Object))
                        {
                            yield return clause;
                        }
                    }
                    else
                    {
                        foreach (Clause clause in Get())
                        {
                            yield return clause;
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in Match(null))
            {
                sb.AppendLine(entry.ToString());
            }
            return sb.ToString();
        }

        static IDictionary<T1, Tuple<IDictionary<T2, ISet<T3>>, IDictionary<T3, ISet<T2>>>>
        AddIndex<T1, T2, T3>(IDictionary<T1, Tuple<IDictionary<T2, ISet<T3>>, IDictionary<T3, ISet<T2>>>> index, T1 v1, T2 v2, T3 v3)
        {
            Tuple<IDictionary<T2, ISet<T3>>, IDictionary<T3, ISet<T2>>> by1;
            if (!index.TryGetValue(v1, out by1))
            {
                by1 = Tuple.Create<IDictionary<T2, ISet<T3>>, IDictionary<T3, ISet<T2>>>(
                    new Dictionary<T2, ISet<T3>>(),
                    new Dictionary<T3, ISet<T2>>());
                index.Add(v1, by1);
            }

            ISet<T3> by2;
            if (!by1.Item1.TryGetValue(v2, out by2))
            {
                by2 = new HashSet<T3>();
                by1.Item1.Add(v2, by2);
            }
            by2.Add(v3);

            ISet<T2> by3;
            if (!by1.Item2.TryGetValue(v3, out by3))
            {
                by3 = new HashSet<T2>();
                by1.Item2.Add(v3, by3);
            }
            by3.Add(v2);

            return index;
        }

        IEnumerable<Clause> GetBySubjectPredicateObject(Name s, Name p, Value o)
        {
            Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>> i;
            if (_s.TryGetValue(s, out i))
            {
                ISet<Value> j;
                if (i.Item1.TryGetValue(p, out j))
                {
                    if (j.Contains(o))
                    {
                        yield return new Clause { Subject = s, Predicate = p, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Clause> GetBySubjectPredicate(Name s, Name p)
        {
            Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>> i;
            if (_s.TryGetValue(s, out i))
            {
                ISet<Value> j;
                if (i.Item1.TryGetValue(p, out j))
                {
                    foreach (var o in j)
                    {
                        yield return new Clause { Subject = s, Predicate = p, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Clause> GetBySubjectObject(Name s, Value o)
        {
            Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>> i;
            if (_s.TryGetValue(s, out i))
            {
                ISet<Name> j;
                if (i.Item2.TryGetValue(o, out j))
                {
                    foreach (var p in j)
                    {
                        yield return new Clause { Subject = s, Predicate = p, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Clause> GetBySubject(Name s)
        {
            Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>> i;
            if (_s.TryGetValue(s, out i))
            {
                foreach (var p in i.Item1)
                {
                    foreach (var o in p.Value)
                    {
                        yield return new Clause { Subject = s, Predicate = p.Key, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Clause> GetByPredicateObject(Name p, Value o)
        {
            Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>> i;
            if (_p.TryGetValue(p, out i))
            {
                ISet<Name> j;
                if (i.Item2.TryGetValue(o, out j))
                {
                    foreach (var s in j)
                    {
                        yield return new Clause { Subject = s, Predicate = p, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Clause> GetByPredicate(Name p)
        {
            Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>> i;
            if (_p.TryGetValue(p, out i))
            {
                foreach (var s in i.Item1)
                {
                    foreach (var o in s.Value)
                    {
                        yield return new Clause { Subject = s.Key, Predicate = p, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Clause> GetByObject(Value o)
        {
            Tuple<IDictionary<Name, ISet<Name>>, IDictionary<Name, ISet<Name>>> i;
            if (_o.TryGetValue(o, out i))
            {
                foreach (var s in i.Item1)
                {
                    foreach (var p in s.Value)
                    {
                        yield return new Clause { Subject = s.Key, Predicate = p, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Clause> Get()
        {
            foreach (var s in _s)
            {
                foreach (var p in s.Value.Item1)
                {
                    foreach (var o in p.Value)
                    {
                        yield return new Clause { Subject = s.Key, Predicate = p.Key, Object = o };
                    }
                }
            }
        }
    }
}
