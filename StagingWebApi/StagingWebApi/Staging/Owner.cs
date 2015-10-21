using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace StagingWebApi
{
    class Owner : LinkedDataDocument
    {
        IDictionary<string, Stage> _stages;

        public Owner(string address, string name) : base(address, "Owner")
        {
            Name = name;
            _stages = new Dictionary<string, Stage>(StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; set; }

        public override void WriteJson(JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
            WriteResource(jsonWriter);
            jsonWriter.WritePropertyName("name");
            jsonWriter.WriteValue(Name);
            jsonWriter.WritePropertyName("stages");
            jsonWriter.WriteStartArray();
            foreach (var stage in _stages.Values)
            {
                stage.WriteJson(jsonWriter);
            }
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }

        public Stage Add(string name)
        {
            Stage stage;
            if (!_stages.TryGetValue(name, out stage))
            {
                stage = new Stage(BaseAddress + "/" + name.ToLowerInvariant(), name);
                _stages.Add(name, stage);
            }
            return stage;
        }

        public void Add(string name, string id, string version, DateTime staged, string nuspecLocation, string packageOwner)
        {
            Add(name).Add(id, version, staged, nuspecLocation, packageOwner);
        }

        public static string MakeRelativeUri(string ownerName)
        {
            return ownerName.ToLowerInvariant();
        }
    }
}
