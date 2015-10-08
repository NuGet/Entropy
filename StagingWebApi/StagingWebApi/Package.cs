using Newtonsoft.Json;
using System.Collections.Generic;

namespace StagingWebApi
{
    class Package
    {
        string _address;
        string _id;
        List<string> _versions;

        public Package(string address, string id)
        {
            _address = address;
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
            jsonWriter.WriteValue(_address);
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
                jsonWriter.WriteValue(_address + "/" + version.ToLowerInvariant());
                jsonWriter.WritePropertyName("version");
                jsonWriter.WriteValue(version);
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }
    }
}
