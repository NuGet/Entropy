using Newtonsoft.Json;
<<<<<<< HEAD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
=======
using System.Collections.Generic;
using System.IO;
using System.Linq;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d

namespace StagingWebApi
{
    public class Index
    {
        IDictionary<string, Description> _resources;

        public Index()
        {
            _resources = new Dictionary<string, Description>();
        }

        public void Add(string resource, string propertyName, string propertyValue)
        {
            Description description;
            if (!_resources.TryGetValue(resource, out description))
            {
                description = new Description();
                _resources.Add(resource, description);
            }
            description.Add(propertyName, propertyValue);
        }

        public string ToJson()
        {
<<<<<<< HEAD
            using (TextWriter writer = new StringWriter())
            {
                using (JsonWriter jsonWriter = new JsonTextWriter(writer))
                {
=======
            using (var writer = new StringWriter())
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    jsonWriter.Formatting = Formatting.Indented;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
                    WriteJson(jsonWriter);
                    jsonWriter.Flush();
                    writer.Flush();
                    return writer.ToString();
                }
            }
        }

        public void WriteJson(JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
<<<<<<< HEAD

            jsonWriter.WritePropertyName("version");
            jsonWriter.WriteValue("3.0.0-beta.1");

=======
            jsonWriter.WritePropertyName("version");
            jsonWriter.WriteValue("3.0.0-beta.1");
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
            jsonWriter.WritePropertyName("resources");
            jsonWriter.WriteStartArray();
            foreach (var resource in _resources)
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("@id");
                jsonWriter.WriteValue(resource.Key);
                resource.Value.WriteJson(jsonWriter);
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndArray();
<<<<<<< HEAD

=======
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
            jsonWriter.WriteEndObject();
        }

        class Description
        {
            IDictionary<string, List<string>> _properties;

            public Description()
            {
                _properties = new Dictionary<string, List<string>>();
            }

            public void Add(string propertyName, string propertyValue)
            {
                List<string> propertyValues;
                if (!_properties.TryGetValue(propertyName, out propertyValues))
                {
                    propertyValues = new List<string>();
                    _properties.Add(propertyName, propertyValues);
                }
                propertyValues.Add(propertyValue);
            }

            public void WriteJson(JsonWriter jsonWriter)
            {
                foreach (var property in _properties)
                {
                    jsonWriter.WritePropertyName(property.Key);
                    if (property.Value.Count == 1)
                    {
                        jsonWriter.WriteValue(property.Value.First());
                    }
                    else
                    {
                        jsonWriter.WriteStartArray();
                        foreach (string value in property.Value)
                        {
                            jsonWriter.WriteValue(value);
                        }
                        jsonWriter.WriteEndArray();
                    }
                }
            }
        }
    }
}
