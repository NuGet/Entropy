using System;
using System.Diagnostics;
using System.Net;
using CommandLine;
using LogReplay.Commands;

namespace LogReplay
{
    class Program
    {
        static int Main(string[] args)
        {
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            Trace.Listeners.Add(new ColorConsoleTraceListener());

            PrintHeader();
            
            return Parser.Default.ParseArguments<RunOptions>(args)
                .MapResult(
                    (RunOptions opts) => AsyncHelper.RunSync(() => RunCommand.ExecuteAsync(opts)),
                    errs => 1);
        }

        private static void PrintHeader()
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  0");
            Console.WriteLine(" /\\=,---.");
            Console.WriteLine("/\\   `O'");
            Console.WriteLine();
            Console.ForegroundColor = currentColor;
        }
    }
}
