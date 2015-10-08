using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace StagingWebApi
{
    public class Stage
    {
        string _address;
        string _name;
        IDictionary<string, Package> _packages;

        public Stage(string address, string name)
        {
            _address = address;
            _name = name;
            _packages = new Dictionary<string, Package>(StringComparer.OrdinalIgnoreCase);
        }

        public void WriteJson(JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("@id");
            jsonWriter.WriteValue(_address);
            jsonWriter.WritePropertyName("@type");
            jsonWriter.WriteValue("Stage");
            jsonWriter.WritePropertyName("name");
            jsonWriter.WriteValue(_name);
            jsonWriter.WritePropertyName("packages");
            jsonWriter.WriteStartArray();
            foreach (var package in _packages.Values)
            {
                package.WriteJson(jsonWriter);
            }
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }

        public void Add(string id, string version)
        {
            Package package;
            if (!_packages.TryGetValue(id, out package))
            {
                package = new Package(_address + "/" + id.ToLowerInvariant(), id);
                _packages.Add(id, package);
            }
            package.Add(version);
        }
    }
}