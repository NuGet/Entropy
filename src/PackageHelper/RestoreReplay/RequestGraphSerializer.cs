using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace PackageHelper.RestoreReplay
{
    static class RequestGraphSerializer
    {
        public static void WriteToFile(string path, RequestGraph graph)
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

                    if (node.HitIndex != default)
                    {
                        j.WritePropertyName("h");
                        j.WriteValue(node.HitIndex);
                    }

                    if (node.StartRequest.Method != "GET")
                    {
                        j.WritePropertyName("m");
                        j.WriteValue(node.StartRequest.Method);
                    }

                    j.WritePropertyName("u");
                    j.WriteValue(node.StartRequest.Url);

                    if (node.EndRequest != null)
                    {
                        if (node.EndRequest.StatusCode != HttpStatusCode.OK)
                        {
                            j.WritePropertyName("c");
                            j.WriteValue((int)node.EndRequest.StatusCode);
                        }

                        j.WritePropertyName("d");
                        j.WriteValue(node.EndRequest.Duration.Ticks);
                    }

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

        public static RequestGraph ReadFromFile(string path)
        {
            var nodes = new List<RequestNode>();
            var indexToNode = new Dictionary<int, RequestNode>();
            var nodeToDependencyIndexes = new Dictionary<RequestNode, List<int>>();

            var serializer = new JsonSerializer();

            using (var stream = File.OpenRead(path))
            using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
            using (var textReader = new StreamReader(gzipStream))
            using (var j = new JsonTextReader(textReader))
            {
                j.Read();
                Assert(j.TokenType == JsonToken.StartObject, "The first token should be the start of an object.");
                j.Read();
                while (j.TokenType == JsonToken.PropertyName)
                {
                    switch ((string)j.Value)
                    {
                        case "n":
                            j.Read();
                            Assert(j.TokenType == JsonToken.StartArray, "The first token of the 'n' property should be the start of an array.");
                            j.Read();
                            while (j.TokenType == JsonToken.StartObject)
                            {
                                var node = ReadRequestNode(serializer, j, out var dependencyIndexes);
                                nodes.Add(node);
                                indexToNode.Add(indexToNode.Count, node);
                                nodeToDependencyIndexes.Add(node, dependencyIndexes);

                                Assert(j.TokenType == JsonToken.EndObject, "The last token the request node shoudl be the end of an array.");
                                j.Read();
                            }
                            Assert(j.TokenType == JsonToken.EndArray, "The last token of the 'n' property should be the end of an array.");
                            break;

                    }

                    j.Read();
                }
                while (j.TokenType == JsonToken.PropertyName);

                Assert(j.TokenType == JsonToken.EndObject, "The last token should be the end of an object.");
            }

            foreach (var node in nodes)
            {
                foreach (var index in nodeToDependencyIndexes[node])
                {
                    node.Dependencies.Add(indexToNode[index]);
                }
            }

            return new RequestGraph(nodes);
        }

        private static RequestNode ReadRequestNode(JsonSerializer serializer, JsonReader j, out List<int> dependencyIndexes)
        {
            var hitIndex = default(int);
            var method = "GET";
            string url = null;
            var statusCode = HttpStatusCode.OK;
            TimeSpan? duration = null;
            dependencyIndexes = new List<int>();

            j.Read();
            while (j.TokenType == JsonToken.PropertyName)
            {
                switch ((string)j.Value)
                {
                    case "h":
                        hitIndex = j.ReadAsInt32().Value;
                        break;
                    case "m":
                        method = j.ReadAsString();
                        break;
                    case "u":
                        url = j.ReadAsString();
                        break;
                    case "c":
                        statusCode = (HttpStatusCode)j.ReadAsInt32().Value;
                        break;
                    case "d":
                        j.Read();
                        Assert(j.TokenType == JsonToken.Integer, "The 'd' property should be an integer.");
                        duration = TimeSpan.FromTicks((long)j.Value);
                        break;
                    case "e":
                        j.Read();
                        dependencyIndexes = serializer.Deserialize<List<int>>(j);
                        break;
                }

                j.Read();
            }

            var node = new RequestNode(
                hitIndex,
                new StartRequest(method, url),
                new HashSet<RequestNode>(HitIndexAndUrlComparer.Instance));

            if (duration != null)
            {
                node.EndRequest = new EndRequest(statusCode, url, duration ?? TimeSpan.Zero);
            }

            return node;
        }

        private static void Assert(bool assertion, string message)
        {
            if (!assertion)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
