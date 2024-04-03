using System;

namespace RestoreTraceParser
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if(args.Length != 2)
            {
                Console.WriteLine("Usage: RestoreTraceParser <action> <path to trace files>");
                Console.WriteLine("Valid actions:");
                Console.WriteLine("\tpt: Breaks down the total restore time by project and phase.");
                Console.WriteLine("\tgs: Provides statistics about the graph produced by RemoteDependencyWalker.");
                return;
            }

            if (args[0].Equals("pt"))
            {
                PhaseTracker.Execute(args);
            }
            else if (args[0].Equals("gs"))
            {
                GraphStats.Execute(args);
            }
        }
    }
}