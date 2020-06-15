using find_buids_in_sprint.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace find_buids_in_sprint
{
    class Program
    {
        // todo: read this from somewhere, not in code, to minimise risk of accidentally commiting secrets
        private const string accountName = "";
        private const string personalAccessToken = "";

        static async Task Main(string[] args)
        {
            var startTime = new DateTimeOffset(2020, 05, 23, 0, 0, 0, TimeSpan.FromHours(-7));
            var endTime = new DateTimeOffset(2020, 06, 12, 0, 0, 0, TimeSpan.FromHours(-7));

            if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(personalAccessToken))
            {
                Console.WriteLine("Set account name and PAT");
                return;
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("BASIC", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(accountName + ":" + personalAccessToken)));

            //using var request = new HttpRequestMessage(HttpMethod.Get, "https://dev.azure.com/devdiv/devdiv/_apis/build/builds?definitions=8117&api-version=5.1"); // Official build
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://dev.azure.com/devdiv/devdiv/_apis/build/builds?definitions=8118&api-version=5.1"); // Private build

            var response = await client.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using (Stream stream = await response.Content.ReadAsStreamAsync())
            {
                var result = await System.Text.Json.JsonSerializer.DeserializeAsync<BuildList>(stream);

                var builds = result.value
                    .Where(b => b.finishTime >= startTime && b.finishTime <= endTime)
                    .Where(b => b.result == "failed" || b.result == "canceled")
                    .ToList();

                var summary = builds.Select(b => new
                {
                    buildId = b.id,
                    buildVersion = b.buildNumber,
                    url = b.links["web"].href,
                    result = b.result,
                    date = b.finishTime,
                    issues = new List<string>()
                })
                    .ToList();

                var options = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };


                var json = JsonSerializer.Serialize(summary, options);
                Console.WriteLine(json);


                var rates = result.value
                    .Where(b => b.finishTime >= startTime && b.finishTime <= endTime)
                    .Aggregate<BuildInfo, Dictionary<string, int>>(
                    seed: new Dictionary<string, int>(),
                    func: (dict, build) =>
                    {
                        var key = build.result;
                        if (!dict.TryGetValue(key, out int count))
                        {
                            count = 0;
                        }
                        count++;
                        dict[key] = count;
                        return dict;
                    });

                var totalBuilds = rates.Sum(r => r.Value);
                foreach (var kvp in rates)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}/{totalBuilds} ({kvp.Value * 100.0 / totalBuilds}");
                }
            }
        }
    }
}
