using System;
using PackageWithSymbols;

namespace SymbolEgressTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var a = TestClass.Add(2, 3);
            Console.WriteLine("Result: {0}", a);
        }
    }
}
