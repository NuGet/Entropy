using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace GithubIssueTagger.GraphQL
{
    internal class GitHubGraphQLClient
    {
        public GitHubGraphQLClient(GitHubPat pat, string userAgent)
        {
            if (pat == null)
            {
                throw new ArgumentNullException(nameof(pat));
            }
            if (pat.Value == null)
            {
                throw new ArgumentException(paramName: nameof(pat), message: "Property Value cannot be null");
            }

            HttpClient = new HttpClient();
            HttpClient.BaseAddress = new Uri("https://api.github.com/graphql");
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("BEARER", pat.Value);
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(userAgent, "0.1"));
        }

        public HttpClient HttpClient { get; }

        public async Task<GraphQLResponse<T>?> SendAsync<T>(GraphQLRequest request)
        {
            using var httpResponse = await HttpClient.PostAsJsonAsync(requestUri: (string?)null, request);

            Console.WriteLine(httpResponse);

            var rateLimitLimit = httpResponse.Headers.GetValues("X-RateLimit-Limit").SingleOrDefault();
            var rateLimitRemaining = httpResponse.Headers.GetValues("X-RateLimit-Remaining").SingleOrDefault();
            var rateLimitReset = long.Parse(httpResponse.Headers.GetValues("X-RateLimit-Reset").Single());
            var resetTime = DateTime.UnixEpoch.AddSeconds(rateLimitReset);
            Console.WriteLine($"GraphQL HTTP rate-limit = {rateLimitLimit}, remaining = {rateLimitRemaining}, reset = {resetTime:o}");

            using var stream = await httpResponse.Content.ReadAsStreamAsync();
            GraphQLResponse<T>? response = await JsonSerializer.DeserializeAsync<GraphQLResponse<T>>(stream);

            return response;
        }
    }
}
