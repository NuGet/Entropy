using System;
using System.Collections.Generic;
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
                Description = "Convert a request graph to an operation graph, or vice versa",
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
                    Arity = ArgumentArity.OneOrMore,
                },
            });
            command.Add(new Option<bool>("--write-graphviz")
            {
                Description = "Output Graphviz DOT files (.gv) in addition to serialized graph"
            });
            command.Add(new Option<string>("--variant-name")
            {
                Description = "Force a specific variant name on the output file",
            });
            command.Add(new Option<bool>("--no-variant-name")
            {
                Description = "Force no variant name on the output file",
            });

            command.Handler = CommandHandler.Create<string, List<string>, bool, string, bool>(ExecuteAsync);

            return command;
        }

        static async Task<int> ExecuteAsync(string path, List<string> sources, bool writeGraphviz, string variantName, bool noVariantName)
        {
            Console.WriteLine("Parsing the file name...");

            if (!path.EndsWith(GraphSerializer.FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"The serialized graph must have the extension {GraphSerializer.FileExtension}");
                return 1;
            }

            if (!Helper.TryParseFileName(path, out var graphType, out var parsedVariantName, out var solutionName))
            {
                graphType = null;
                parsedVariantName = null;
                solutionName = null;
            }

            Console.WriteLine($"  Graph type:    {graphType ?? "(none)"}");
            Console.WriteLine($"  Variant name:  {parsedVariantName ?? "(none)"}");
            Console.WriteLine($"  Solution name: {solutionName ?? "(none)"}");

            if (noVariantName)
            {
                parsedVariantName = null;
                Console.WriteLine("Using no variant name because of command line switch.");
            }

            var actualVariantName = parsedVariantName;
            if (variantName != null)
            {
                actualVariantName = variantName;
                Console.WriteLine($"Using variant name '{variantName}' from command line option.");
            }

            var dir = Path.GetDirectoryName(path);

            if (graphType == RequestGraph.Type)
            {
                var requestGraph = ParseGraph<RequestGraph, RequestNode>(path, RequestGraphSerializer.ReadFromFile);

                Console.WriteLine("Converting the request graph to an operation graph...");
                sources = sources == null || !sources.Any() ? requestGraph.Sources : sources;
                var operationGraph = await GraphConverter.ToOperationGraphAsync(requestGraph, sources);

                var filePath = Path.Combine(dir, Helper.GetGraphFileName(OperationGraph.Type, actualVariantName, solutionName));

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
            else if (graphType == OperationGraph.Type)
            {
                var operationGraph = ParseGraph<OperationGraph, OperationNode>(path, OperationGraphSerializer.ReadFromFile);

                Console.WriteLine("Converting the operation graph to request graph...");
                var requestGraph = await GraphConverter.ToRequestGraphAsync(operationGraph, sources);

                var filePath = Path.Combine(dir, Helper.GetGraphFileName(RequestGraph.Type, actualVariantName, solutionName));

                if (writeGraphviz)
                {
                    var gvPath = $"{filePath}.gv";
                    Console.WriteLine($"  Writing {gvPath}...");
                    RequestGraphSerializer.WriteToGraphvizFile(gvPath, requestGraph);
                }

                var jsonGzPath = $"{filePath}{GraphSerializer.FileExtension}";
                Console.WriteLine($"  Writing {jsonGzPath}...");
                RequestGraphSerializer.WriteToFile(jsonGzPath, requestGraph);

                return 0;
            }
            else
            {
                Console.WriteLine($"The input graph type '{graphType}' is not supported.");
                return 1;
            }
        }

        private static TGraph ParseGraph<TGraph, TNode>(string path, Func<string, TGraph> readFromFile)
            where TGraph : IGraph<TNode>
            where TNode : INode<TNode>
        {
            Console.WriteLine("Parsing the graph...");
            var graph = readFromFile(path);
            Console.WriteLine($"  There are {graph.Nodes.Count} nodes.");
            Console.WriteLine($"  There are {graph.Nodes.Sum(x => x.Dependencies.Count)} edges.");
            return graph;
        }
    }
}
