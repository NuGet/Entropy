using Structures;
using System;
using System.Collections.Generic;

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

            g.Assert(new Name("a"), new Name("x"), "1");
            g.Assert(new Name("a"), new Name("y"), "2");
            g.Assert(new Name("a"), new Name("z"), "3");
            g.Assert(new Name("b"), new Name("x"), "1");
            g.Assert(new Name("b"), new Name("y"), "2");
            g.Assert(new Name("b"), new Name("z"), "3");
            g.Assert(new Name("b"), new Name("z"), "4");
            g.Assert(new Name("b"), new Name("z"), "5");
            g.Assert(new Name("c"), new Name("x"), "1");
            g.Assert(new Name("c"), new Name("y"), "2");
            g.Assert(new Name("c"), new Name("y"), "3");
            g.Assert(new Name("c"), new Name("z"), "3");
            g.Assert(new Name("c"), new Name("z"), new Name("d"));
            g.Assert(new Name("c"), new Name("z"), new Name("e"));
            g.Assert(new Name("d"), new Name("z"), "4");
            g.Assert(new Name("d"), new Name("z"), "5");
            g.Assert(new Name("e"), new Name("z"), "6");

            Test(g, Triple.Empty);
            Test(g, new Triple(new Name("a"), null, null));
            Test(g, new Triple(new Name("b"), new Name("z"), null));
            Test(g, new Triple(new Name("c"), null, "3"));
            Test(g, new Triple(new Name("c"), new Name("z"), "3"));
            Test(g, new Triple(null, new Name("z"), null));
            Test(g, new Triple(null, new Name("z"), "3"));
            Test(g, new Triple(null, null, "3"));
        }

        static void Test1()
        {
            Graph g = new Graph();

            g.Assert(new Name("a"), new Name("x"), "1");
            g.Assert(new Name("a"), new Name("y"), "2");
            g.Assert(new Name("a"), new Name("z"), "3");
            g.Assert(new Name("b"), new Name("x"), "1");
            g.Assert(new Name("b"), new Name("y"), "2");
            g.Assert(new Name("b"), new Name("z"), "3");
            g.Assert(new Name("b"), new Name("z"), "4");
            g.Assert(new Name("b"), new Name("z"), "5");
            g.Assert(new Name("c"), new Name("x"), "1");
            g.Assert(new Name("c"), new Name("y"), "2");
            g.Assert(new Name("c"), new Name("y"), "3");
            g.Assert(new Name("c"), new Name("z"), "3");
            g.Assert(new Name("c"), new Name("z"), new Name("d"));
            g.Assert(new Name("c"), new Name("z"), new Name("e"));
            g.Assert(new Name("d"), new Name("z"), "4");
            g.Assert(new Name("d"), new Name("z"), "5");
            g.Assert(new Name("e"), new Name("z"), "6");

            Graph q = new Graph();
            q.Assert(new Name("c"), new Name("z"), new Variable("v0"));
            q.Assert(new Variable("v0"), new Name("z"), new Variable("v1"));


            foreach (var binding in Query.Select(g, q))
            {
                foreach (var entry in binding)
                {
                    Console.WriteLine("{0} : {1}", entry.Key, entry.Value);
                }
                Console.WriteLine("----------------------");
            }
        }

        static void Test2()
        {
            Graph g = new Graph();

            g.Assert(new Name("a"), new Name("x"), "1");
            g.Assert(new Name("a"), new Name("y"), "2");
            g.Assert(new Name("a"), new Name("z"), "3");
            g.Assert(new Name("b"), new Name("x"), "1");
            g.Assert(new Name("b"), new Name("y"), "2");
            g.Assert(new Name("b"), new Name("z"), "3");
            g.Assert(new Name("b"), new Name("z"), "4");
            g.Assert(new Name("b"), new Name("z"), "5");
            g.Assert(new Name("c"), new Name("x"), "1");
            g.Assert(new Name("c"), new Name("y"), "2");
            g.Assert(new Name("c"), new Name("y"), "3");
            g.Assert(new Name("c"), new Name("z"), "3");
            g.Assert(new Name("c"), new Name("z"), new Name("d"));
            g.Assert(new Name("c"), new Name("z"), new Name("e"));
            g.Assert(new Name("d"), new Name("z"), "4");
            g.Assert(new Name("d"), new Name("z"), "5");
            g.Assert(new Name("e"), new Name("z"), "6");

            Graph q = new Graph();
            q.Assert(new Name("c"), new Name("z"), new Variable("v0"));
            q.Assert(new Variable("v0"), new Name("z"), new Variable("v1"));

            Graph t = new Graph();
            t.Assert(new Name("o"), new Name("v0"), new Variable("v0"));
            t.Assert(new Name("o"), new Name("v1"), new Variable("v1"));

            IGraph r = Query.Construct(g, q, t);

            foreach (var entry in r.Match(Triple.Empty))
            {
                Console.WriteLine(entry);
            }
        }

        static IGraph CreatePackage(string id, string version)
        {
            string packageUri = string.Format("{0}/{1}", id, version);
            IGraph g = new Graph();
            g.Assert(new Name(packageUri), new Name("id"), id);
            g.Assert(new Name(packageUri), new Name("version"), version);
            return g;
        }

        static void Test3()
        {
            IGraph g = new Graph();
            g.Add(CreatePackage("ef", "1.0.0"));
            g.Add(CreatePackage("ef", "2.0.0"));
            g.Add(CreatePackage("ef", "3.0.0"));
            g.Add(CreatePackage("ef", "4.0.0"));

            IGraph q = new Graph();
            q.Assert(new Variable("package"), new Name("id"), new Variable("id"));
            q.Assert(new Variable("package"), new Name("version"), new Variable("version"));

            IGraph t = new Graph();
            t.Assert(new Variable("registration"), new Name("id"), new Variable("id"));
            t.Assert(new Variable("registration"), new Name("version"), new Variable("version"));

            Func<IDictionary<string, object>, object> func = (scope) =>
            {
                return new Name(string.Format("http://nuget.org/package/{0}", scope["id"]));
            };

            var p = new Dictionary<string, object>
            {
                { "registration", func },
            };

            IGraph r = Query.Construct(g, q, t, p);

            foreach (var entry in r.Match(Triple.Empty))
            {
                Console.WriteLine(entry);
            }
        }

        static void Test4()
        {
            Graph g = new Graph();

            g.Assert(new Name("a"), new Name("x"), "1");
            g.Assert(new Name("a"), new Name("y"), "2");
            g.Assert(new Name("a"), new Name("z"), "3");
            g.Assert(new Name("b"), new Name("x"), "1");
            g.Assert(new Name("b"), new Name("y"), "2");
            g.Assert(new Name("b"), new Name("z"), "3");
            g.Assert(new Name("b"), new Name("z"), "4");
            g.Assert(new Name("b"), new Name("z"), "5");
            g.Assert(new Name("c"), new Name("x"), "1");
            g.Assert(new Name("c"), new Name("y"), "2");
            g.Assert(new Name("c"), new Name("y"), "3");
            g.Assert(new Name("c"), new Name("z"), "3");
            g.Assert(new Name("c"), new Name("z"), new Name("d"));
            g.Assert(new Name("c"), new Name("z"), new Name("e"));
            g.Assert(new Name("d"), new Name("z"), "4");
            g.Assert(new Name("d"), new Name("z"), "5");
            g.Assert(new Name("e"), new Name("z"), "6");

            foreach (var s in g.List())
            {
                Console.WriteLine(s);
                foreach (var p in g.List(s))
                {
                    Console.WriteLine("\t{0}", p);
                    foreach (var o in g.List(s, p))
                    {
                        Console.WriteLine("\t\t{0}", o);
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
                //Test0();
                //Test1();
                //Test2();
                //Test3();
                Test4();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
