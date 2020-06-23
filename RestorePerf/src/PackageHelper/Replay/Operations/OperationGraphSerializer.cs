using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace PackageHelper.Replay.Operations
{
    static class OperationGraphSerializer
    {
        public static void WriteToFile(string path, OperationGraph graph)
        {
            GraphSerializer.WriteToFile<OperationGraph, OperationNode>(
                path,
                graph,
                (h, g) => { },
                WriteNode);
        }

        private static void WriteNode(JsonTextWriter j, OperationNode node)
        {
            if (node.HitIndex != default)
            {
                j.WritePropertyName("h");
                j.WriteValue(node.HitIndex);
            }

            if (node.Operation.SourceIndex != default)
            {
                j.WritePropertyName("s");
                j.WriteValue(node.Operation.SourceIndex);
            }

            j.WritePropertyName("t");
            j.WriteValue((int)node.Operation.Type);

            switch (node.Operation)
            {
                case OperationWithIdVersion operationWithIdVersion:
                    j.WritePropertyName("i");
                    j.WriteValue(operationWithIdVersion.Id);
                    j.WritePropertyName("v");
                    j.WriteValue(operationWithIdVersion.Version);
                    break;
                case OperationWithId operationWithId:
                    j.WritePropertyName("i");
                    j.WriteValue(operationWithId.Id);
                    break;
                default:
                    throw new NotImplementedException($"Operation type {node.Operation.Type} is not supported for serialization.");
            }
        }

        public static OperationGraph ReadFromFile(string path)
        {
            return GraphSerializer.ReadFromFile(
                path,
                new OperationGraph(),
                (s, j, g) => { },
                ReadNode);
        }

        private static OperationNode ReadNode(JsonSerializer serializer, JsonReader j, List<int> dependencyIndexes)
        {
            var hitIndex = default(int);
            var sourceIndex = default(int);
            OperationType? type = null;
            string id = null;
            string version = null;

            j.Read();
            while (j.TokenType == JsonToken.PropertyName)
            {
                switch ((string)j.Value)
                {
                    case "h":
                        hitIndex = j.ReadAsInt32().Value;
                        break;
                    case "s":
                        sourceIndex = j.ReadAsInt32().Value;
                        break;
                    case "t":
                        type = (OperationType)j.ReadAsInt32().Value;
                        break;
                    case "i":
                        id = j.ReadAsString();
                        break;
                    case "v":
                        version = j.ReadAsString();
                        break;
                    case "e":
                        j.Read();
                        dependencyIndexes.AddRange(serializer.Deserialize<List<int>>(j));
                        break;
                }

                j.Read();
            }

            if (!type.HasValue)
            {
                throw new InvalidDataException("The 't' property is required for operation nodes.");
            }

            Operation operation;
            switch (type)
            {
                case OperationType.PackageBaseAddressIndex:
                    operation = new OperationWithId(sourceIndex, type.Value, id);
                    break;
                case OperationType.PackageBaseAddressNupkg:
                    operation = new OperationWithIdVersion(sourceIndex, type.Value, id, version);
                    break;
                default:
                    throw new NotImplementedException($"Operation type {type} is not supported for deserialization.");
            }

            return new OperationNode(hitIndex, operation);
        }

        public static void WriteToGraphvizFile(string path, OperationGraph graph)
        {
            var builder = new StringBuilder();
            GraphSerializer.WriteToGraphvizFile(
                path,
                graph,
                n => GetNodeLabel(builder, n));
        }

        private static string GetNodeLabel(StringBuilder builder, OperationNode node)
        {
            builder.Clear();

            switch (node.Operation.Type)
            {
                case OperationType.PackageBaseAddressIndex:
                    var packageBaseAddressIndex = (OperationWithId)node.Operation;
                    builder.AppendFormat("{0}/index.json", packageBaseAddressIndex.Id);
                    break;
                case OperationType.PackageBaseAddressNupkg:
                    var packageBaseAddressNupkg = (OperationWithIdVersion)node.Operation;
                    builder.AppendFormat("{0}.{1}.nupkg", packageBaseAddressNupkg.Id, packageBaseAddressNupkg.Version);
                    break;
                default:
                    throw new NotImplementedException($"Operation type {node.Operation.Type} is not supported for serialization.");
            }

            return builder.ToString();
        }
    }
}
