using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace StagingWebApi
{
    public abstract class LinkedDataDocument
    {
        string _id;
        string _type;

        protected LinkedDataDocument(string id, string type)
        {
            _id = id;
            _type = type;
        }

        public string BaseAddress { get { return _id; } }

        public string ToJson()
        {
            using (var textWriter = new StringWriter())
            {
                using (var jsonWriter = new JsonTextWriter(textWriter))
                {
                    jsonWriter.Formatting = Formatting.Indented;
                    WriteJson(jsonWriter);
                    jsonWriter.Flush();
                    textWriter.Flush();
                    return textWriter.ToString();
                }
            }
        }
        public abstract void WriteJson(JsonWriter jsonWriter);

        protected void WriteResource(JsonWriter jsonWriter)
        {
            jsonWriter.WritePropertyName("@id");
            jsonWriter.WriteValue(_id);
            jsonWriter.WritePropertyName("@type");
            jsonWriter.WriteValue(_type);
        }
    }
}