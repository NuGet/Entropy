using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace StagingWebApi
{
    public class Stage : LinkedDataDocument
    {
        IDictionary<string, Package> _packages;

        public Stage(string address, string name) : base(address, "Stage")
        {
            Name = name;
            _packages = new Dictionary<string, Package>(StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; set; }

        public override void WriteJson(JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
            WriteResource(jsonWriter);
            jsonWriter.WritePropertyName("name");
            jsonWriter.WriteValue(Name);
            jsonWriter.WritePropertyName("packages");
            jsonWriter.WriteStartArray();
            foreach (var package in _packages.Values)
            {
                package.WriteJson(jsonWriter);
            }
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }

        public void Add(string id, string version, DateTime staged, string nuspecLocation, string packageOwner)
        {
            Package package;
            if (!_packages.TryGetValue(id, out package))
            {
                package = new Package(BaseAddress + "/" + id.ToLowerInvariant(), id);
                _packages.Add(id, package);
            }
            package.Add(version, staged, nuspecLocation, packageOwner);
        }
        public static string MakeRelativeUri(string ownerName, string stageName)
        {
            return string.Format("{0}/{1}", Owner.MakeRelativeUri(ownerName), stageName).ToLowerInvariant();
        }
    }
}