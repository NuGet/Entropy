using System;
using System.Threading;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || !int.TryParse(args[0], out var sleepDuration))
            {
                sleepDuration = 1000;
            }

            Console.WriteLine($"Waiting for {sleepDuration}ms...");
            Thread.Sleep(sleepDuration);
        }
    }
}
