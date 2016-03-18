using System;
using System.Linq;

namespace Merge
{
    static class MergeTests
    {
        public static void Test0()
        {
            var x = new string[] { };
            var y = new string[] { };

            foreach (var s in x.Merge(y, (sx, sy) => { return string.Compare(sx, sy); }))
            {
                Console.Write($"{s} ");
            }
            Console.WriteLine();
            Console.WriteLine("----------------------------");
        }

        public static void Test1()
        {
            var x = new[] { "a", "c", "d", "i", "k", "s", "w", "x", "z" };
            var y = new string[] { };

            foreach (var s in x.Merge(y, (sx, sy) => { return string.Compare(sx, sy); }))
            {
                Console.Write($"{s} ");
            }
            Console.WriteLine();
            Console.WriteLine("----------------------------");
        }

        public static void Test2()
        {
            var x = new string[] { };
            var y = new[] { "b", "j", "q", "t", "y" };

            foreach (var s in x.Merge(y, (sx, sy) => { return string.Compare(sx, sy); }))
            {
                Console.Write($"{s} ");
            }
            Console.WriteLine();
            Console.WriteLine("----------------------------");
        }

        public static void Test3()
        {
            var x = new[] { "a", "c", "d", "i", "j", "k", "s", "w", "x", "z" };
            var y = new[] { "b", "q", "t", "y" };

            foreach (var s in x.Merge(y, (sx, sy) => { return string.Compare(sx, sy); }))
            {
                Console.Write($"{s} ");
            }
            Console.WriteLine();
            Console.WriteLine("----------------------------");
        }

        public static void Test4()
        {
            var data = new[]
            {
                new[] { "e", "i", "n", "r", "t", "y" },
                new[] { "b", "f", "j", "m", "p", "z" },
                new[] { "a", "c", "g", "l", "o", "u", "x" },
                new[] { "q", "s", "v", "w" },
                new[] { "d", "h", "k" },
            };

            Func<string, string, int> f = (sx, sy) => { return string.Compare(sx, sy); };

            var acc = Enumerable.Empty<string>();
            foreach (var l in data)
            {
                acc = acc.Merge(l, f);
            }

            foreach (var i in acc)
            {
                Console.Write($"{i} ");
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------");
        }
    }
}
