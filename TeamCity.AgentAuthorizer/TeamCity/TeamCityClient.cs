using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NuGet.TeamCity.AgentAuthorizer
{
    public class TeamCityClient
    {
        private readonly HttpMessageHandler _handler;
        private readonly HttpClient _httpClient;
        private readonly Uri _serverUrl;

        public TeamCityClient(Uri serverUrl)
        {
            _serverUrl = serverUrl;

            _handler = new LoggingHandler
            {
                InnerHandler = new HttpClientHandler
                {
                    UseDefaultCredentials = true
                }
            };

            _httpClient = new HttpClient(_handler);
        }

        private async Task<JToken> SendAsync(HttpMethod method, string endpoint, HttpContent content = null)
        {
            var requestUrl = new Uri(_serverUrl, endpoint);
            var request = new HttpRequestMessage(method, requestUrl);

            var acceptJson = new MediaTypeWithQualityHeaderValue("application/json");
            request.Headers.Accept.Add(acceptJson);

            if (content != null)
            {
                request.Content = content;
            }

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<JToken>(json);
        }

        public async Task<IReadOnlyList<Agent>> GetAgentsAsync()
        {
            var endpoint = "/app/rest/agents?" +
                "locator=authorized:any&" +
                "fields=agent(id,name,typeId,connected,authorized,href)";
            var json = await SendAsync(HttpMethod.Get, endpoint);
            return DeserializeResponseAsAgents(json);
        }

        public async Task<IReadOnlyList<AgentPool>> GetAgentPoolsAsync()
        {
            var endpoint = $"/app/rest/agentPools";
            var json = await SendAsync(HttpMethod.Get, endpoint);
            return json["agentPool"]
                .Select(x => x.ToObject<AgentPool>())
                .ToList();
        }

        public async Task<IReadOnlyList<Agent>> GetAgentPoolAgentsAsync(int agentPoolId)
        {
            var endpoint = $"/app/rest/agentPools/id:{agentPoolId}/agents";
            var json = await SendAsync(HttpMethod.Get, endpoint);
            return json["agent"]
                .Select(x => x.ToObject<Agent>())
                .ToList();
        }

        public async Task AddAgentToAgentPoolAsync(int agentPoolId, int agentId)
        {
            var xml = new XDocument("agent");
            xml.Root.Add(new XAttribute("id", agentId));

            var content = new StringContent(xml.ToString());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

            var json = await SendAsync(HttpMethod.Put, $"/app/rest/agentPools/id:{agentPoolId}/agents", content);
            Console.WriteLine(json);
        }

        public async Task SetAgentAuthorizationAsync(int agentId, bool authorized)
        {
            var content = new StringContent(authorized.ToString());
            content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

            var json = await SendAsync(HttpMethod.Put, $"/app/rest/agents/id:{agentId}/authorized", content);
            Console.WriteLine(json);
        }

        private static IReadOnlyList<Agent> DeserializeResponseAsAgents(JToken json)
        {
            return json["agent"]
                .Select(x => x.ToObject<Agent>())
                .ToList();
        }
    }
}
