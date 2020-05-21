using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using PackageHelper.Commands;

namespace PackageHelper
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 64;
            ThreadPool.SetMinThreads(workerThreads: 64, completionPortThreads: 4);

            var commands = new SortedDictionary<string, Func<string[], Task<int>>>(StringComparer.OrdinalIgnoreCase)
            {
                { DownloadAllVersions.Name, DownloadAllVersions.ExecuteAsync },
                { ParseRestoreLogs.Name, ParseRestoreLogs.ExecuteAsync },
                { Push.Name, Push.ExecuteAsync },
                { ReplayRequestGraph.Name, ReplayRequestGraph.ExecuteAsync },
            };

            if (args.Length > 0 && commands.TryGetValue(args[0], out var command))
            {
                try
                {
                    return await command(args.Skip(1).ToArray());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An exception was thrown by command {command}.");
                    Console.WriteLine(ex.ToString());
                    return 1;
                }
            }

            Console.WriteLine("The first argument must be a supported command: ");
            Console.WriteLine($"  {string.Join(" | ", commands.Keys)}");
            return 1;
        }

    }
}
