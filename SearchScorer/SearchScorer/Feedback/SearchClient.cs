using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace SearchScorer.Feedback
{
    public class SearchClient
    {
        private readonly HttpClient _httpClient;

        public SearchClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<SearchResponse> SearchAsync(string baseUrl, string query, int take)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["q"] = query;
            queryString["prerelease"] = "true";
            queryString["semVerLevel"] = "2.0.0";
            queryString["take"] = take.ToString();

            var uriBuilder = new UriBuilder(baseUrl)
            {
                Path = "/query",
                Query = queryString.ToString(),
            };

            var requestUri = uriBuilder.Uri;

            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            using (var response = await _httpClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<SearchResponse>(json);
            }
        }
    }
}
