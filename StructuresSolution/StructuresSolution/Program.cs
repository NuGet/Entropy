using Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructuresSolution
{
    class Program
    {
        static void Main(string[] args)
        {
            Document document = new Document();

            document.Assert(new[]
            {
                new Entry(new Name("a"), new Name("x"), new Value("1")),
                new Entry(new Name("a"), new Name("y"), new Value("2")),
                new Entry(new Name("a"), new Name("z"), new Value("3")),
                new Entry(new Name("b"), new Name("x"), new Value("1")),
                new Entry(new Name("b"), new Name("y"), new Value("2")),
                new Entry(new Name("b"), new Name("z"), new Value("3")),
                new Entry(new Name("c"), new Name("x"), new Value("1")),
                new Entry(new Name("c"), new Name("y"), new Value("2")),
                new Entry(new Name("c"), new Name("z"), new Value("3")),
            });

            foreach (var entry in document.Match(null))
            {
                Console.WriteLine(entry);
            }
        }
    }
}
