using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Structures
{
    public class Graph : IGraph
    {
        IDictionary<object, Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>>> _s;
        IDictionary<object, Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>>> _p;
        IDictionary<object, Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>>> _o;

        public Graph()
        {
            _s = new Dictionary<object, Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>>>();
            _p = new Dictionary<object, Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>>>();
            _o = new Dictionary<object, Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>>>();
        }

        public void Assert(object s, object p, object o)
        {
            _s = AddIndex(_s, s, p, o);
            _p = AddIndex(_p, p, s, o);
            _o = AddIndex(_o, o, s, p);
        }

        public void Retract(Triple fact)
        {
            if (fact == null || fact.Subject == null || fact.Predicate == null || fact.Object == null)
            {
                throw new ArgumentNullException("fact");
            }
        }

        public void Add(IGraph g)
        {
            foreach (var fact in g.Match(Triple.Empty))
            {
                Assert(fact.Subject, fact.Predicate, fact.Object);
            }
        }

        public IEnumerable<Triple> Match(Triple partial)
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
                        foreach (Triple clause in GetBySubjectPredicateObject(partial.Subject, partial.Predicate, partial.Object))
                        {
                            yield return clause;
                        }
                    }
                    else
                    {
                        foreach (Triple clause in GetBySubjectPredicate(partial.Subject, partial.Predicate))
                        {
                            yield return clause;
                        }
                    }
                }
                else
                {
                    if (partial.Object != null)
                    {
                        foreach (Triple clause in GetBySubjectObject(partial.Subject, partial.Object))
                        {
                            yield return clause;
                        }
                    }
                    else
                    {
                        foreach (Triple clause in GetBySubject(partial.Subject))
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
                        foreach (Triple clause in GetByPredicateObject(partial.Predicate, partial.Object))
                        {
                            yield return clause;
                        }
                    }
                    else
                    {
                        foreach (Triple clause in GetByPredicate(partial.Predicate))
                        {
                            yield return clause;
                        }
                    }
                }
                else
                {
                    if (partial.Object != null)
                    {
                        foreach (Triple clause in GetByObject(partial.Object))
                        {
                            yield return clause;
                        }
                    }
                    else
                    {
                        foreach (Triple clause in Get())
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

        public IEnumerable<object> List()
        {
            foreach (var s in _s.Keys)
            {
                yield return s;
            }
        }
        public IEnumerable<object> List(object s)
        {
            foreach (var p in _s[s].Item1.Keys)
            {
                yield return p;
            }
        }
        public IEnumerable<object> List(object s, object p)
        {
            foreach (var o in _s[s].Item1[p])
            {
                yield return o;
            }
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

        IEnumerable<Triple> GetBySubjectPredicateObject(object s, object p, object o)
        {
            Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>> i;
            if (_s.TryGetValue(s, out i))
            {
                ISet<object> j;
                if (i.Item1.TryGetValue(p, out j))
                {
                    if (j.Contains(o))
                    {
                        yield return new Triple { Subject = s, Predicate = p, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Triple> GetBySubjectPredicate(object s, object p)
        {
            Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>> i;
            if (_s.TryGetValue(s, out i))
            {
                ISet<object> j;
                if (i.Item1.TryGetValue(p, out j))
                {
                    foreach (var o in j)
                    {
                        yield return new Triple { Subject = s, Predicate = p, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Triple> GetBySubjectObject(object s, object o)
        {
            Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>> i;
            if (_s.TryGetValue(s, out i))
            {
                ISet<object> j;
                if (i.Item2.TryGetValue(o, out j))
                {
                    foreach (var p in j)
                    {
                        yield return new Triple { Subject = s, Predicate = p, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Triple> GetBySubject(object s)
        {
            Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>> i;
            if (_s.TryGetValue(s, out i))
            {
                foreach (var p in i.Item1)
                {
                    foreach (var o in p.Value)
                    {
                        yield return new Triple { Subject = s, Predicate = p.Key, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Triple> GetByPredicateObject(object p, object o)
        {
            Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>> i;
            if (_p.TryGetValue(p, out i))
            {
                ISet<object> j;
                if (i.Item2.TryGetValue(o, out j))
                {
                    foreach (var s in j)
                    {
                        yield return new Triple { Subject = s, Predicate = p, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Triple> GetByPredicate(object p)
        {
            Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>> i;
            if (_p.TryGetValue(p, out i))
            {
                foreach (var s in i.Item1)
                {
                    foreach (var o in s.Value)
                    {
                        yield return new Triple { Subject = s.Key, Predicate = p, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Triple> GetByObject(object o)
        {
            Tuple<IDictionary<object, ISet<object>>, IDictionary<object, ISet<object>>> i;
            if (_o.TryGetValue(o, out i))
            {
                foreach (var s in i.Item1)
                {
                    foreach (var p in s.Value)
                    {
                        yield return new Triple { Subject = s.Key, Predicate = p, Object = o };
                    }
                }
            }
            yield break;
        }
        IEnumerable<Triple> Get()
        {
            foreach (var s in _s)
            {
                foreach (var p in s.Value.Item1)
                {
                    foreach (var o in p.Value)
                    {
                        yield return new Triple { Subject = s.Key, Predicate = p.Key, Object = o };
                    }
                }
            }
        }
    }
}
