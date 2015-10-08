using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace StagingWebApi
{
    class Owner
    {
        string _baseAddress;
        string _name;

        IDictionary<string, Stage> _stages;

        public Owner(string baseAddress, string name)
        {
            _baseAddress = baseAddress;
            _name = name;
            _stages = new Dictionary<string, Stage>(StringComparer.OrdinalIgnoreCase);
        }

        public void WriteJson(JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("@id");
            jsonWriter.WriteValue((_baseAddress + _name).ToLowerInvariant());
            jsonWriter.WritePropertyName("@type");
            jsonWriter.WriteValue("Owner");
            jsonWriter.WritePropertyName("name");
            jsonWriter.WriteValue(_name);
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
                stage = new Stage(_baseAddress + _name + "/", name);
                _stages.Add(name, stage);
            }
            return stage;
        }

        public void Add(string name, string id, string version)
        {
            Add(name).Add(id, version);
        }

        public string ToJson()
        {
            using (StringWriter textWriter = new StringWriter())
            {
                using (JsonTextWriter jsonWriter = new JsonTextWriter(textWriter))
                {
                    jsonWriter.Formatting = Formatting.Indented;
                    WriteJson(jsonWriter);
                    jsonWriter.Flush();
                    textWriter.Flush();
                    return textWriter.ToString();
                }
            }
        }
    }
}
