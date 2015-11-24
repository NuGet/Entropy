using System;
using System.Collections.Generic;
using System.Linq;

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
            foreach (var fact in partial)
            {
                Retract(fact);
            }
        }
        public IEnumerable<Entry> Match(IEnumerable<Entry> partial)
        {
            return Enumerable.Empty<Entry>();
        }

        void Assert(Entry fact)
        {
            if (fact.Subject == null || fact.Predicate == null || fact.Object == null)
            {
                throw new ArgumentException("fact");
            }

            if (_s == null)
            {
                _s = new Dictionary<Name, Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>>>();
                _p = new Dictionary<Name, Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>>>();
                _o = new Dictionary<Value, Tuple<IDictionary<Name, ISet<Name>>, IDictionary<Name, ISet<Name>>>>();
            }

            Tuple<IDictionary<Name, ISet<Value>>, IDictionary<Value, ISet<Name>>> bySubject;
            if (_s.TryGetValue(fact.Subject, out bySubject))
            {
            }
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
