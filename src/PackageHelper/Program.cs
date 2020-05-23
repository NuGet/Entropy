using System.CommandLine;
using System.CommandLine.Invocation;
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

            var rootCommand = new RootCommand();

            rootCommand.Add(DownloadAllVersions.GetCommand());
            rootCommand.Add(Push.GetCommand());
            rootCommand.Add(ParseRestoreLogs.GetCommand());
            rootCommand.Add(ReplayRequestGraph.GetCommand());

            return await rootCommand.InvokeAsync(args);
        }

    }
}
