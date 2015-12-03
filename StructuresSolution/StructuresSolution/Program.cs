using Structures;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace StructuresSolution
{
    class Program
    {
        static void Test(Graph document, Triple clause)
        {
            Console.WriteLine(clause.Display());
            foreach (var entry in document.Match(clause))
            {
                Console.WriteLine(entry);
            }
        }

        static void Test0()
        {
            Graph g = new Graph();

            var ns = XNamespace.Get("http://nuget.org/schema#");

            g.Assert(new Triple(ns.GetName("a"), ns.GetName("x"), new Value("1")));
            g.Assert(new Triple(ns.GetName("a"), ns.GetName("y"), new Value("2")));
            g.Assert(new Triple(ns.GetName("a"), ns.GetName("z"), new Value("3")));
            g.Assert(new Triple(ns.GetName("b"), ns.GetName("x"), new Value("1")));
            g.Assert(new Triple(ns.GetName("b"), ns.GetName("y"), new Value("2")));
            g.Assert(new Triple(ns.GetName("b"), ns.GetName("z"), new Value("3")));
            g.Assert(new Triple(ns.GetName("b"), ns.GetName("z"), new Value("4")));
            g.Assert(new Triple(ns.GetName("b"), ns.GetName("z"), new Value("5")));
            g.Assert(new Triple(ns.GetName("c"), ns.GetName("x"), new Value("1")));
            g.Assert(new Triple(ns.GetName("c"), ns.GetName("y"), new Value("2")));
            g.Assert(new Triple(ns.GetName("c"), ns.GetName("y"), new Value("3")));
            g.Assert(new Triple(ns.GetName("c"), ns.GetName("z"), new Value("3")));
            g.Assert(new Triple(ns.GetName("c"), ns.GetName("z"), new Value(ns.GetName("d"))));
            g.Assert(new Triple(ns.GetName("c"), ns.GetName("z"), new Value(ns.GetName("e"))));
            g.Assert(new Triple(ns.GetName("d"), ns.GetName("z"), new Value("4")));
            g.Assert(new Triple(ns.GetName("d"), ns.GetName("z"), new Value("5")));
            g.Assert(new Triple(ns.GetName("e"), ns.GetName("z"), new Value("6")));

            Test(g, Triple.Empty);
            Test(g, new Triple(ns.GetName("a"), null, null));
            Test(g, new Triple(ns.GetName("b"), ns.GetName("z"), null));
            Test(g, new Triple(ns.GetName("c"), null, new Value("3")));
            Test(g, new Triple(ns.GetName("c"), ns.GetName("z"), new Value("3")));
            Test(g, new Triple(null, ns.GetName("z"), null));
            Test(g, new Triple(null, ns.GetName("z"), new Value("3")));
            Test(g, new Triple(null, null, new Value("3")));
        }

        /*
        static void Test1()
        {
            Graph g = new Graph();

            g.Assert(new Triple(new Name("a"), new Name("x"), new Value("1")));
            g.Assert(new Triple(new Name("a"), new Name("y"), new Value("2")));
            g.Assert(new Triple(new Name("a"), new Name("z"), new Value("3")));
            g.Assert(new Triple(new Name("b"), new Name("x"), new Value("1")));
            g.Assert(new Triple(new Name("b"), new Name("y"), new Value("2")));
            g.Assert(new Triple(new Name("b"), new Name("z"), new Value("3")));
            g.Assert(new Triple(new Name("b"), new Name("z"), new Value("4")));
            g.Assert(new Triple(new Name("b"), new Name("z"), new Value("5")));
            g.Assert(new Triple(new Name("c"), new Name("x"), new Value("1")));
            g.Assert(new Triple(new Name("c"), new Name("y"), new Value("2")));
            g.Assert(new Triple(new Name("c"), new Name("y"), new Value("3")));
            g.Assert(new Triple(new Name("c"), new Name("z"), new Value("3")));
            g.Assert(new Triple(new Name("c"), new Name("z"), new Value(new Name("d"))));
            g.Assert(new Triple(new Name("c"), new Name("z"), new Value(new Name("e"))));
            g.Assert(new Triple(new Name("d"), new Name("z"), new Value("4")));
            g.Assert(new Triple(new Name("d"), new Name("z"), new Value("5")));
            g.Assert(new Triple(new Name("e"), new Name("z"), new Value("6")));

            Query query = new Query();

            query.Add(new QueryName(new Name("c")), new QueryName(new Name("z")), new QueryValue("v0"));
            query.Add(new QueryName("v0"), new QueryName(new Name("z")), new QueryValue("v1"));

            foreach (var binding in query.Select(g))
            {
                foreach (var entry in binding)
                {
                    Console.WriteLine("{0} : {1}", entry.Key, entry.Value);
                }
                Console.WriteLine("----------------------");
            }
        }
        */

        static void Test2()
        {
            XNamespace ns0 = "http://tempuri.org/lala#";
            XName a0 = ns0.GetName("a");
            XName b0 = ns0.GetName("b");

            XNamespace ns1 = "http://tempuri.org/deda#";
            XName a1 = ns1.GetName("a");
            XName b1 = ns1.GetName("b");

            var d = new Dictionary<XName, string>();

            d.Add(a0, "1");
            d.Add(b0, "2");
            d.Add(a1, "3");
            d.Add(b1, "4");

            foreach (var entry in d)
            {
                Console.WriteLine("{0} = {1}", entry.Key, entry.Value);
            }
        }

        /*
        static void Test3()
        {
            Graph g = new Graph();

            g.Assert(new Triple(new Name("a"), new Name("x"), new Value("1")));
            g.Assert(new Triple(new Name("a"), new Name("y"), new Value("2")));
            g.Assert(new Triple(new Name("a"), new Name("z"), new Value("3")));
            g.Assert(new Triple(new Name("b"), new Name("x"), new Value("1")));
            g.Assert(new Triple(new Name("b"), new Name("y"), new Value("2")));
            g.Assert(new Triple(new Name("b"), new Name("z"), new Value("3")));
            g.Assert(new Triple(new Name("b"), new Name("z"), new Value("4")));
            g.Assert(new Triple(new Name("b"), new Name("z"), new Value("5")));
            g.Assert(new Triple(new Name("c"), new Name("x"), new Value("1")));
            g.Assert(new Triple(new Name("c"), new Name("y"), new Value("2")));
            g.Assert(new Triple(new Name("c"), new Name("y"), new Value("3")));
            g.Assert(new Triple(new Name("c"), new Name("z"), new Value("3")));
            g.Assert(new Triple(new Name("c"), new Name("z"), new Value(new Name("d"))));
            g.Assert(new Triple(new Name("c"), new Name("z"), new Value(new Name("e"))));
            g.Assert(new Triple(new Name("d"), new Name("z"), new Value("4")));
            g.Assert(new Triple(new Name("d"), new Name("z"), new Value("5")));
            g.Assert(new Triple(new Name("e"), new Name("z"), new Value("6")));

            Query query = new Query();

            query.Add(new QueryName(new Name("c")), new QueryName(new Name("z")), new QueryValue("v0"));
            query.Add(new QueryName("v0"), new QueryName(new Name("z")), new QueryValue("v1"));

            IGraph result = query.Construct(g);

            foreach (var entry in result.Match(Triple.Empty))
            {
                Console.WriteLine(entry);
            }
        }
        */

        static void Test3()
        {
            Graph g = new Graph();

            var ns = XNamespace.Get("http://nuget.org/schema#");

            g.Assert(new Triple(ns.GetName("a"), ns.GetName("x"), new Value("1")));
            g.Assert(new Triple(ns.GetName("a"), ns.GetName("y"), new Value("2")));
            g.Assert(new Triple(ns.GetName("a"), ns.GetName("z"), new Value("3")));
            g.Assert(new Triple(ns.GetName("b"), ns.GetName("x"), new Value("1")));
            g.Assert(new Triple(ns.GetName("b"), ns.GetName("y"), new Value("2")));
            g.Assert(new Triple(ns.GetName("b"), ns.GetName("z"), new Value("3")));
            g.Assert(new Triple(ns.GetName("b"), ns.GetName("z"), new Value("4")));
            g.Assert(new Triple(ns.GetName("b"), ns.GetName("z"), new Value("5")));
            g.Assert(new Triple(ns.GetName("c"), ns.GetName("x"), new Value("1")));
            g.Assert(new Triple(ns.GetName("c"), ns.GetName("y"), new Value("2")));
            g.Assert(new Triple(ns.GetName("c"), ns.GetName("y"), new Value("3")));
            g.Assert(new Triple(ns.GetName("c"), ns.GetName("z"), new Value("3")));
            g.Assert(new Triple(ns.GetName("c"), ns.GetName("z"), new Value(ns.GetName("d"))));
            g.Assert(new Triple(ns.GetName("c"), ns.GetName("z"), new Value(ns.GetName("e"))));
            g.Assert(new Triple(ns.GetName("d"), ns.GetName("z"), new Value("4")));
            g.Assert(new Triple(ns.GetName("d"), ns.GetName("z"), new Value("5")));
            g.Assert(new Triple(ns.GetName("e"), ns.GetName("z"), new Value("6")));

            Query query = new Query();

            query.Add(ns.GetName("c"), ns.GetName("z"), new Variable("v0"));
            query.Add(new Variable("v0"), ns.GetName("z"), new Variable("v1"));

            IGraph result = query.Construct(g);

            foreach (var entry in result.Match(Triple.Empty))
            {
                Console.WriteLine(entry);
            }
        }

        static void Main(string[] args)
        {
            try
            {
                //Test0();
                //Test1();
                //Test2();
                Test3();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
