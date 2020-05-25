using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using PackageHelper.Replay;
using PackageHelper.Replay.Requests;

namespace PackageHelper.Commands
{
    static class ConvertGraph
    {
        public static Command GetCommand()
        {
            var command = new Command("convert-graph")
            {
                Description = "Convert a request graph to a NuGet operation graph, or vice versa",
            };

            command.Add(new Argument<string>("path")
            {
                Description = "Path to a serialized graph",
            });

            command.Handler = CommandHandler.Create<string>(ExecuteAsync);

            return command;
        }

        static async Task ExecuteAsync(string path)
        {
            var requestGraph = RequestGraphSerializer.ReadFromFile(path);
            var sources = new[] { "https://api.nuget.org/v3/index.json" };
            var operationGraph = await GraphConverter.ToOperationGraphAsync(sources, requestGraph);
        }
    }
}
