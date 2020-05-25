using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;

namespace PackageHelper.Replay
{
    static class GraphSerializer
    {
        public const string FileExtension = ".json.gz";

        public static void WriteToGraphvizFile<TNode>(
            string path,
            IGraph<TNode> graph,
            Func<TNode, string> getNodeLabel) where TNode : INode<TNode>
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("digraph G {");
                writer.WriteLine("  node [fontsize=16];");

                var roots = new List<TNode>();
                foreach (var node in graph.Nodes.OrderBy(x => getNodeLabel(x), StringComparer.Ordinal))
                {
                    foreach (var dependency in node.Dependencies.OrderBy(x => getNodeLabel(x), StringComparer.Ordinal))
                    {
                        writer.Write("  ");
                        WriteGraphvizNode(writer, dependency, getNodeLabel);
                        writer.Write(" -> ");
                        WriteGraphvizNode(writer, node, getNodeLabel);
                        writer.WriteLine(" [dir=back];");
                    }

                    if (node.Dependencies.Count == 0)
                    {
                        roots.Add(node);
                    }
                }

                writer.WriteLine("  {");
                writer.WriteLine("    rank=same;");
                foreach (var node in roots.OrderBy(x => getNodeLabel(x), StringComparer.Ordinal))
                {
                    writer.Write("    ");
                    WriteGraphvizNode(writer, node, getNodeLabel);
                    writer.WriteLine(";");
                }
                writer.WriteLine("  }");

                writer.WriteLine("}");
            }
        }

        private static void WriteGraphvizNode<TNode>(
            TextWriter writer,
            TNode node,
            Func<TNode, string> getNodeLabel)
        {
            writer.Write("\"");
            writer.Write(getNodeLabel(node));
            writer.Write("\"");
        }

        public static void WriteToFile<TNode>(
            string path,
            IGraph<TNode> graph,
            Action<JsonTextWriter, TNode> writeNode) where TNode : INode<TNode>
        {
            var nodeToIndex = graph
                .Nodes
                .Select((n, i) => new { Node = n, Index = i })
                .ToDictionary(x => x.Node, x => x.Index);

            using (var stream = new FileStream(path, FileMode.Create))
            using (var gzipStream = new GZipStream(stream, CompressionLevel.Optimal))
            using (var writer = new StreamWriter(gzipStream))
            using (var j = new JsonTextWriter(writer))
            {
                j.WriteStartObject();
                j.WritePropertyName("n");
                j.WriteStartArray();

                foreach (var node in graph.Nodes)
                {
                    j.WriteStartObject();

                    writeNode(j, node);
                    if (node.Dependencies.Count > 0)
                    {
                        j.WritePropertyName("e");
                        j.WriteStartArray();
                        var indexes = node.Dependencies.Select(x => nodeToIndex[x]).OrderBy(x => x);
                        foreach (var index in indexes)
                        {
                            j.WriteValue(index);
                        }
                        j.WriteEndArray();
                    }

                    j.WriteEndObject();
                }

                j.WriteEndArray();
                j.WriteEndObject();
            }
        }

        public static void ReadFromFile<TNode>(
            string path,
            IGraph<TNode> graph,
            Func<JsonSerializer, JsonReader, List<int>, TNode> readNode) where TNode : INode<TNode>
        {
            var indexToNode = new Dictionary<int, TNode>();
            var nodeToDependencyIndexes = new Dictionary<TNode, List<int>>();

            var serializer = new JsonSerializer();

            using (var stream = File.OpenRead(path))
            using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
            using (var textReader = new StreamReader(gzipStream))
            using (var j = new JsonTextReader(textReader))
            {
                j.Read();
                Helper.Assert(j.TokenType == JsonToken.StartObject, "The first token should be the start of an object.");
                j.Read();
                while (j.TokenType == JsonToken.PropertyName)
                {
                    switch ((string)j.Value)
                    {
                        case "n":
                            j.Read();
                            Helper.Assert(j.TokenType == JsonToken.StartArray, "The first token of the 'n' property should be the start of an array.");
                            j.Read();
                            while (j.TokenType == JsonToken.StartObject)
                            {
                                var dependencyIndexes = new List<int>();
                                var node = readNode(serializer, j, dependencyIndexes);
                                graph.Nodes.Add(node);
                                indexToNode.Add(indexToNode.Count, node);
                                nodeToDependencyIndexes.Add(node, dependencyIndexes);

                                Helper.Assert(j.TokenType == JsonToken.EndObject, "The last token the request node should be the end of an array.");
                                j.Read();
                            }
                            Helper.Assert(j.TokenType == JsonToken.EndArray, "The last token of the 'n' property should be the end of an array.");
                            break;

                    }

                    j.Read();
                }
                while (j.TokenType == JsonToken.PropertyName);

                Helper.Assert(j.TokenType == JsonToken.EndObject, "The last token should be the end of an object.");
            }

            foreach (var node in graph.Nodes)
            {
                foreach (var index in nodeToDependencyIndexes[node])
                {
                    node.Dependencies.Add(indexToNode[index]);
                }
            }
        }
    }
}
