using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace PackageHelper.Replay.Requests
{
    static class RequestGraphSerializer
    {
        public static void WriteToFile(string path, RequestGraph graph)
        {
            GraphSerializer.WriteToFile<RequestGraph, RequestNode>(
                path,
                graph,
                WriteGraphProperties,
                WriteNode);
        }

        private static void WriteGraphProperties(JsonTextWriter j, RequestGraph graph)
        {
            if (graph.Sources.Any())
            {
                j.WritePropertyName("s");
                j.WriteStartArray();
                foreach (var source in graph.Sources)
                {
                    j.WriteValue(source);
                }
                j.WriteEndArray();
            }
        }

        private static void WriteNode(JsonTextWriter j, RequestNode node)
        {
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
        }

        public static RequestGraph ReadFromFile(string path)
        {
            return GraphSerializer.ReadFromFile(
                path,
                new RequestGraph(),
                ReadGraphProperty,
                ReadNode);
        }

        private static void ReadGraphProperty(JsonSerializer serializer, JsonReader j, RequestGraph graph)
        {
            switch ((string)j.Value)
            {
                case "s":
                    j.Read();
                    graph.Sources.AddRange(serializer.Deserialize<List<string>>(j));
                    break;
            }
        }

        private static RequestNode ReadNode(JsonSerializer serializer, JsonReader j, List<int> dependencyIndexes)
        {
            var hitIndex = default(int);
            var method = "GET";
            string url = null;
            var statusCode = HttpStatusCode.OK;
            TimeSpan? duration = null;

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
                        Helper.Assert(j.TokenType == JsonToken.Integer, "The 'd' property should be an integer.");
                        duration = TimeSpan.FromTicks((long)j.Value);
                        break;
                    case "e":
                        j.Read();
                        dependencyIndexes.AddRange(serializer.Deserialize<List<int>>(j));
                        break;
                }

                j.Read();
            }

            var node = new RequestNode(hitIndex, new StartRequest(method, url));

            if (duration != null)
            {
                node.EndRequest = new EndRequest(statusCode, url, duration ?? TimeSpan.Zero);
            }

            return node;
        }

        public static void WriteToGraphvizFile(string path, RequestGraph graph)
        {
            var builder = new StringBuilder();
            GraphSerializer.WriteToGraphvizFile(
                path,
                graph,
                n => GetNodeLabel(builder, n));
        }

        private static string GetNodeLabel(StringBuilder builder, RequestNode node)
        {
            builder.Clear();

            if (node.HitIndex != 0)
            {
                builder.AppendFormat("({0}) ", node.HitIndex);
            }

            var label = node.StartRequest.Url;
            if (label.EndsWith("/index.json"))
            {
                label = string.Join("/", label.Split('/').Reverse().Take(2).Reverse());
            }
            else if (label.EndsWith(".nupkg"))
            {
                label = label.Split('/').Last();
            }

            builder.Append(label);

            return builder.ToString();
        }

    }
}
