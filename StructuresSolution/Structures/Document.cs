using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Structures
{
    public class Document : IDocument
    {
        IDictionary<Name, Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>>> _s;
        IDictionary<Name, Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>>> _p;
        IDictionary<Value, Tuple<IDictionary<Name, ISet<Name>>, IDictionary<Name, ISet<Name>>>> _o;

        public Document()
        {
        }

        public void Assert(IEnumerable<Entry> facts)
        {
            foreach (var fact in facts)
            {
                Assert(fact);
            }
        }
        public void Retract(IEnumerable<Entry> partial)
        {
            foreach (var fact in Match(partial))
            {
                Retract(fact);
            }
        }
        public IEnumerable<Entry> Match(IEnumerable<Entry> partial)
        {
            Entry entry = partial.FirstOrDefault();
            if (entry == null || (entry.Subject == null && entry.Predicate == null && entry.Object == null))
            {
                foreach (var s in _s)
                {
                    foreach (var p in s.Value.Item1)
                    {
                        foreach (var o in p.Value)
                        {
                            yield return new Entry { Subject = s.Key, Predicate = p.Key, Object = o };
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

        void Assert(Entry fact)
        {
            if (fact.Subject == null || fact.Predicate == null || fact.Object == null)
            {
                throw new ArgumentException("fact");
            }

            _s = AddIndex(_s, fact.Subject, fact.Predicate, fact.Object);
            _p = AddIndex(_p, fact.Predicate, fact.Subject, fact.Object);
            _o = AddIndex(_o, fact.Object, fact.Subject, fact.Predicate);
        }

        static IDictionary<T1, Tuple<IDictionary<T2, ISet<T3>>, IDictionary<T3, ISet<T2>>>>
        AddIndex<T1, T2, T3>(IDictionary<T1, Tuple<IDictionary<T2, ISet<T3>>, IDictionary<T3, ISet<T2>>>> index, T1 v1, T2 v2, T3 v3)
        {
            if (index == null)
            {
                index = new Dictionary<T1, Tuple<IDictionary<T2, ISet<T3>>, IDictionary<T3, ISet<T2>>>>();
            }

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

        static void AddIndex<T1, T2>()
        {
        }

        void Retract(Entry fact)
        {
            if (_s == null)
            {
                return;
            }
        }
    }
}
