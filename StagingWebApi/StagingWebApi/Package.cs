using Newtonsoft.Json;
using System.Collections.Generic;

namespace StagingWebApi
{
    class Package
    {
        string _baseAddress;
        string _id;
        List<string> _versions;

        public Package(string baseAddress, string id)
        {
            _baseAddress = baseAddress;
            _id = id;
            _versions = new List<string>();
        }

        public void Add(string version)
        {
            _versions.Add(version);
        }

        public void WriteJson(JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("@id");
            jsonWriter.WriteValue((_baseAddress + _id).ToLowerInvariant());
            jsonWriter.WritePropertyName("@type");
            jsonWriter.WriteValue("Package");
            jsonWriter.WritePropertyName("id");
            jsonWriter.WriteValue(_id);
            jsonWriter.WritePropertyName("versions");
            jsonWriter.WriteStartArray();
            foreach (var version in _versions)
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("@id");
                jsonWriter.WriteValue((_baseAddress + _id + "/" + version).ToLowerInvariant());
                jsonWriter.WritePropertyName("version");
                jsonWriter.WriteValue(version);
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }
    }
}
