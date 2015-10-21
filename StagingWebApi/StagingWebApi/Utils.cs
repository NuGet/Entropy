using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
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

        public static HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string reason)
        {
            return new HttpResponseMessage(statusCode) { Content = CreateErrorContent(reason) };
        }

        public static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode) { Content = CreateJsonContent(json) };
        }

        public static Uri CreateBaseAddress(Uri requestUri, string path)
        {
            return new UriBuilder(requestUri.Scheme, requestUri.Host, requestUri.Port, path).Uri;
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

        public static async Task<JObject> LoadResource(HttpClient httpClient, Uri uri, CancellationToken token)
        {
            var response = await httpClient.GetAsync(uri, token);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(json);

            return obj;
        }

        public static async Task CleanUpArtifacts(List<Uri> artifacts)
        {
            PackageStorageBase storage = new AzurePackageStorage();
            foreach (var artifact in artifacts)
            {
                try
                {
                    await storage.Delete(artifact);
                }
                catch (Exception e)
                {
                    Trace.TraceWarning(e.Message);
                }
            }
        }
    }
}
