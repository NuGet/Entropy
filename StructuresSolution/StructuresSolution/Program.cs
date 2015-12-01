using Structures;
using System;

namespace StructuresSolution
{
    class Program
    {
        static void Test(Graph document, Clause clause)
        {
            Console.WriteLine(clause.Display());
            foreach (var entry in document.Match(clause))
            {
                Console.WriteLine(entry);
            }
        }

        static void Main(string[] args)
        {
            Graph g = new Graph();

            g.Assert(new Clause(new Name("a"), new Name("x"), new Value("1")));
            g.Assert(new Clause(new Name("a"), new Name("y"), new Value("2")));
            g.Assert(new Clause(new Name("a"), new Name("z"), new Value("3")));
            g.Assert(new Clause(new Name("b"), new Name("x"), new Value("1")));
            g.Assert(new Clause(new Name("b"), new Name("y"), new Value("2")));
            g.Assert(new Clause(new Name("b"), new Name("z"), new Value("3")));
            g.Assert(new Clause(new Name("b"), new Name("z"), new Value("4")));
            g.Assert(new Clause(new Name("b"), new Name("z"), new Value("5")));
            g.Assert(new Clause(new Name("c"), new Name("x"), new Value("1")));
            g.Assert(new Clause(new Name("c"), new Name("y"), new Value("2")));
            g.Assert(new Clause(new Name("c"), new Name("y"), new Value("3")));
            g.Assert(new Clause(new Name("c"), new Name("z"), new Value("3")));

            Test(g, Clause.Empty);
            Test(g, new Clause(new Name("a"), null, null));
            Test(g, new Clause(new Name("b"), new Name("z"), null));
            Test(g, new Clause(new Name("c"), null, new Value("3")));
            Test(g, new Clause(new Name("c"), new Name("z"), new Value("3")));
            Test(g, new Clause(null, new Name("z"), null));
            Test(g, new Clause(null, new Name("z"), new Value("3")));
            Test(g, new Clause(null, null, new Value("3")));
        }
    }
}
