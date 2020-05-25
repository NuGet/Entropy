using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PackageHelper.Replay;
using PackageHelper.Replay.Operations;
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
            command.Add(new Option("--sources")
            {
                Description = "Package sources to use for graph conversion",
                Argument = new Argument
                {
                    Arity = ArgumentArity.OneOrMore
                },
            });
            command.Add(new Option<bool>("--write-graphviz")
            {
                Description = "Output Graphviz DOT files (.gv) in addition to serialized graph"
            });

            command.Handler = CommandHandler.Create<string, string[], bool>(ExecuteAsync);

            return command;
        }

        static async Task<int> ExecuteAsync(string path, string[] sources, bool writeGraphviz)
        {
            if (sources == null)
            {
                sources = Array.Empty<string>();
            }

            Console.WriteLine("Parsing the file name...");

            if (!path.EndsWith(GraphSerializer.FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"The serialized graph must have the extension {GraphSerializer.FileExtension}");
                return 1;
            }

            if (!Helper.TryParseGraphFileName(path, out var graphType, out var variantName, out var solutionName))
            {
                graphType = null;
                variantName = null;
                solutionName = null;
            }

            Console.WriteLine($"  Graph type:    {graphType ?? "(none)"}");
            Console.WriteLine($"  Variant name:  {variantName ?? "(none)"}");
            Console.WriteLine($"  Solution name: {solutionName ?? "(none)"}");

            var dir = Path.GetDirectoryName(path);

            if (graphType == RequestGraph.Type)
            {
                Console.WriteLine("Parsing the request graph...");
                var graph = RequestGraphSerializer.ReadFromFile(path);
                Console.WriteLine($"  There are {graph.Nodes.Count} nodes.");
                Console.WriteLine($"  There are {graph.Nodes.Sum(x => x.Dependencies.Count)} edges.");

                Console.WriteLine("Converting the request graph to an operation graph...");
                var operationGraph = await GraphConverter.ToOperationGraphAsync(sources, graph);

                var filePath = Path.Combine(dir, Helper.GetGraphFileName(OperationGraph.Type, variantName, solutionName));

                if (writeGraphviz)
                {
                    var gvPath = $"{filePath}.gv";
                    Console.WriteLine($"  Writing {gvPath}...");
                    OperationGraphSerializer.WriteToGraphvizFile(gvPath, operationGraph);
                }

                var jsonGzPath = $"{filePath}{GraphSerializer.FileExtension}";
                Console.WriteLine($"  Writing {jsonGzPath}...");
                OperationGraphSerializer.WriteToFile(jsonGzPath, operationGraph);

                return 0;
            }
            else
            {
                Console.WriteLine($"The input graph type '{graphType}' is not supported.");
                return 1;
            }
        }
    }
}
