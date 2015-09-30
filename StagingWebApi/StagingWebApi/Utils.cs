using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace StagingWebApi
{
    public static class Utils
    {
        public static HttpContent CreateErrorContent(string reason)
        {
            JObject obj = new JObject { { "reason", reason } };
            return CreateJsonContent(obj.ToString());
        }
        public static HttpContent CreateJsonContent(string json)
        {
            HttpContent content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }

        public static IDictionary<string, List<Uri>> MakeIndex(string indexJson)
        {
            JObject indexObj = JObject.Parse(indexJson);

            IDictionary<string, List<Uri>> index = new Dictionary<string, List<Uri>>();

            foreach (JObject resource in indexObj["resources"])
            {
                JToken typeJToken = resource["@type"];

                List<string> types;

                if (typeJToken is JValue)
                {
                    types = new List<string> { typeJToken.ToString() };
                }
                else if (typeJToken is JArray)
                {
                    types = ((JToken)typeJToken).Select((entry) => entry.ToString()).ToList();
                }
                else
                {
                    continue;
                }

                foreach (string type in types)
                {
                    List<Uri> resources;
                    if (!index.TryGetValue(type, out resources))
                    {
                        resources = new List<Uri>();
                        index[type] = resources;
                    }
                    resources.Add(resource["@id"].ToObject<Uri>());
                }
            }

            return index;
        }

        public static async Task<Uri> GetService(Uri indexAddress, string resourceType)
        {
            HttpClient client = new HttpClient();

            string indexJson = await client.GetStringAsync(indexAddress);

            IDictionary<string, List<Uri>> index = MakeIndex(indexJson);

            List<Uri> resources;
            if (index.TryGetValue(resourceType, out resources))
            {
                return resources.First();
            }
            else
            {
                return null;
            }
        }
    }
}
