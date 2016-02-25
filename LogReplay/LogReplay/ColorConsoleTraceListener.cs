using System;
using System.Collections.Generic;
using System.Diagnostics;

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
}