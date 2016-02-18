using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using LogReplay.Commands;

namespace LogReplay
{
    public class ColorConsoleTraceListener
       : ConsoleTraceListener
    {
        private readonly Dictionary<TraceEventType, ConsoleColor> _eventColor = new Dictionary<TraceEventType, ConsoleColor>();

        public ColorConsoleTraceListener()
        {
            _eventColor.Add(TraceEventType.Verbose, ConsoleColor.DarkGray);
            _eventColor.Add(TraceEventType.Information, ConsoleColor.Gray);
            _eventColor.Add(TraceEventType.Warning, ConsoleColor.Yellow);
            _eventColor.Add(TraceEventType.Error, ConsoleColor.Red);
            _eventColor.Add(TraceEventType.Critical, ConsoleColor.Red);
            _eventColor.Add(TraceEventType.Start, ConsoleColor.DarkCyan);
            _eventColor.Add(TraceEventType.Stop, ConsoleColor.DarkCyan);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            TraceEvent(eventCache, source, eventType, id, "{0}", message);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            ConsoleColor originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = GetEventColor(eventType, originalColor);
            base.TraceEvent(eventCache, source, eventType, id, format, args);
            System.Console.ForegroundColor = originalColor;
        }

        private ConsoleColor GetEventColor(TraceEventType eventType, ConsoleColor defaultColor)
        {
            if (!_eventColor.ContainsKey(eventType))
            {
                return defaultColor;
            }

            return _eventColor[eventType];
        }
    }

    public static class AsyncHelper
    {
        private static readonly TaskFactory _taskFactory = new
          TaskFactory(CancellationToken.None,
                      TaskCreationOptions.None,
                      TaskContinuationOptions.None,
                      TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> task)
        {
            return _taskFactory
              .StartNew(task)
              .Unwrap()
              .GetAwaiter()
              .GetResult();
        }

        public static void RunSync(Func<Task> task)
        {
            _taskFactory
              .StartNew(task)
              .Unwrap()
              .GetAwaiter()
              .GetResult();
        }
    }

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
