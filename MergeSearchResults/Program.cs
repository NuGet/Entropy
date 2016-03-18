using System;
using System.Collections.Generic;
using System.Linq;

namespace Merge
{
    class Program
    {
        static Func<string, string, int> CreateScoreComparerFunc(IEnumerable<string> docs)
        {
            //  mock of what Lucene might have produced
            //  the point is that the scorign (and therefore the relatiev ordering) is encapsulated within this function 
            //  the result is a comparer function that can be used as an argument to a generic Merge function

            var score = new Dictionary<string, double>
            {
                { "a", 99.0 },
                { "b", 97.0 },
                { "c", 95.0 },
                { "d", 93.0 },
                { "e", 91.0 },
                { "f", 189.0 },
                { "g", 187.0 },
                { "h", 185.0 },
                { "i", 183.0 },
                { "j", 181.0 },
                { "k", 279.0 },
                { "l", 277.0 },
                { "m", 275.0 },
                { "n", 273.0 },
                { "o", 271.0 },
                { "p", 169.0 },
                { "q", 167.0 },
                { "r", 165.0 },
                { "s", 163.0 },
                { "t", 161.0 },
                { "u", 59.0 },
                { "v", 57.0 },
                { "w", 55.0 },
                { "x", 53.0 },
                { "y", 51.0 },
                { "z", 50.0 },
            };

            return (sx, sy) => { return (score[sx] == score[sy] ? 0 : (score[sx] > score[sy] ? -1 : 1)); };
        }

        static void Test0()
        {
            var l = new List<List<string>>
            {
                new List<string> { "s", "t", "v", "w" },
                new List<string> { "a", "c", "i", "k", "r" },
                new List<string> { "g", "f", "q", "u" },
                new List<string> { "b", "j", "l", "m", "n", "o", "p" },
                new List<string> { "d", "e", "h", "x", "y", "z" },
            };

            //  combines all the data we have and produce a comparer function

            //var f = (sx, sy) => { return string.Compare(sx, sy); };
            var f = CreateScoreComparerFunc(l.SelectMany(i => i));

            //  now execute a merge using this comparer function - note the use of r as an accumulated result

            var r = Enumerable.Empty<string>();
            foreach (var lx in l)
            {
                r = r.Merge(lx, f);
            }

            foreach (var s in r)
            {
                Console.WriteLine(s);
            }

            //  and if we have more results then we need to recreate the comparer function and merge the new results with the existing

            //  the creaton of the comparer function and the merge function are naturally synchronous operations but sit nicely inside an asynchronous world
        }



        static void Test1()
        {
            var test = "MERGESORT";

            var original = test.Select(ch => new string(ch, 1));

            Func<string, string, int> f = (sx, sy) => { return string.Compare(sx, sy); };

            var sorted = original.MergeSort(f);

            foreach (var i in sorted)
            {
                Console.Write($"{i} ");
            }
            Console.WriteLine();
        }

        static void Test2()
        {
            var test = "MICROSOFT";

            var original = test.Select(ch => new string(ch, 1));

            Func<string, string, int> f = (sx, sy) => { return string.Compare(sx, sy); };

            var sorted = original.MergeSort(f);

            foreach (var i in sorted)
            {
                Console.Write($"{i} ");
            }
            Console.WriteLine();
        }

        static void Main(string[] args)
        {
            try
            {
                //Test0();

                Console.WriteLine("Regular Mergesort");

                Test1();
                Test2();

                Console.WriteLine();
                Console.WriteLine("Merge function unit tests");

                MergeTests.Test0();
                MergeTests.Test1();
                MergeTests.Test2();
                MergeTests.Test3();
                MergeTests.Test4();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
