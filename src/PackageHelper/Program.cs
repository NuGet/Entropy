using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PackageHelper
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            ServicePointManager.MaxServicePointIdleTime = 10000;
            ServicePointManager.DefaultConnectionLimit = 64;
            ThreadPool.SetMinThreads(workerThreads: 64, completionPortThreads: 4);

            var commands = new Dictionary<string, Func<string[], Task<int>>>(StringComparer.OrdinalIgnoreCase)
            {
                { DownloadAllVersions.Name, DownloadAllVersions.ExecuteAsync },
                { Push.Name, Push.ExecuteAsync },
            };

            if (args.Length > 0 && commands.TryGetValue(args[0], out var command))
            {
                return await command(args.Skip(1).ToArray());
            }

            Console.WriteLine("The first argument must be a supported command: ");
            Console.WriteLine($"  {string.Join(" | ", commands.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}");
            return 1;
        }

    }
}
