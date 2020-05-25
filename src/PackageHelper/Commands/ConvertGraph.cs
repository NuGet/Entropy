using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using PackageHelper.Parse;
using PackageHelper.Replay;

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
            var requests = requestGraph.Nodes.Select(x => x.StartRequest);

            var operationInfos = await NuGetOperationParser.ParseAsync(sources, requests);
        }
    }
}
