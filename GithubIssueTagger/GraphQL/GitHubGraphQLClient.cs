﻿using System;
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
            HttpResponseMessage? httpResponse;

            int attempt = 0;
            int maxAttempts = 5;
            TimeSpan delay = TimeSpan.FromSeconds(1);

            for (;;)
            {
                httpResponse = null;
                try
                {
                    httpResponse = await HttpClient.PostAsJsonAsync(requestUri: (string?)null, request);

                    var rateLimitLimit = httpResponse.Headers.GetValues("X-RateLimit-Limit").SingleOrDefault();
                    var rateLimitRemaining = httpResponse.Headers.GetValues("X-RateLimit-Remaining").SingleOrDefault();
                    var rateLimitReset = long.Parse(httpResponse.Headers.GetValues("X-RateLimit-Reset").Single());
                    var resetTime = DateTime.UnixEpoch.AddSeconds(rateLimitReset);
                    Console.WriteLine($"GraphQL HTTP rate-limit = {rateLimitLimit}, remaining = {rateLimitRemaining}, reset = {resetTime:o}");

                    using var stream = await httpResponse.Content.ReadAsStreamAsync();
                    GraphQLResponse<T>? response = await JsonSerializer.DeserializeAsync<GraphQLResponse<T>>(stream);

                    httpResponse.Dispose();

                    return response;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GraphQL HTTP error: {ex.Message}");

                    TimeSpan retryAfter = delay;
                    if (httpResponse != null)
                    {
                        Console.WriteLine(httpResponse);

                        if (httpResponse.Content.Headers.ContentLength < 2000)
                        {
                            var body = await httpResponse.Content.ReadAsStringAsync();
                            Console.WriteLine(body);
                        }

                        if (httpResponse.Headers?.RetryAfter?.Date != null)
                        {
                            retryAfter = httpResponse.Headers.RetryAfter.Date.Value - DateTime.UtcNow;
                            if (retryAfter < TimeSpan.Zero)
                            {
                                retryAfter = delay;
                            }
                        }
                        else if (httpResponse.Headers?.RetryAfter?.Delta != null)
                        {
                            retryAfter = httpResponse.Headers.RetryAfter.Delta.Value;
                        }

                        httpResponse.Dispose();
                    }

                    if (attempt >= maxAttempts)
                    {
                        throw;
                    }

                    Console.WriteLine($"GraphQL HTTP retrying in {retryAfter.TotalSeconds} seconds...");
                    await Task.Delay(retryAfter);
                    attempt++;
                    delay += delay;
                }
            }
        }
    }
}
