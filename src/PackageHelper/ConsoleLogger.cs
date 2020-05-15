using NuGet.Common;
using System;
using System.Threading.Tasks;

namespace PackageHelper
{
    public class ConsoleLogger : LoggerBase
    {
        public override void Log(ILogMessage message)
        {
            Console.WriteLine($"[{message.Level.ToString().ToUpperInvariant().Substring(0, 3)}] {message.Message}");
        }

        public override Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }
    }
}
