using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

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
    }
}
