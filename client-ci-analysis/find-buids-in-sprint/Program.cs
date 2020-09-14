using find_buids_in_sprint.Models.AzDO;
using find_buids_in_sprint.Models.ClientCiAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace find_buids_in_sprint
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Expected 1 argument, got " + args.Length);
                return;
            }

            if (File.Exists(args[0]))
            {
                Console.WriteLine("Expected '{0}' to be a directory, but found a file", args[0]);
                return;
            }

            var sprintEpoch = new DateTimeOffset(2010, 07, 26, 0, 0, 0, TimeSpan.FromHours(-7));

            var accountName = Environment.GetEnvironmentVariable("AzDO_ACCOUNT");
            var personalAccessToken = Environment.GetEnvironmentVariable("AzDO_PAT");

            if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(personalAccessToken))
            {
                Console.WriteLine("Set AzDO_ACCOUNT and AzDO_PAT environment variables.");
                Console.WriteLine("Project properties -> Debug in VS");
                Console.WriteLine("launchSettings.json in VSCode");
                return;
            }

            var httpClient = CreateAzureDevOpsClient(accountName, personalAccessToken);

            var cache = new DirectoryInfo(args[0]);
            if (!cache.Exists)
            {
                cache.Create();
            }

            await BuildFetcher.DownloadBuildsAsync(cache, httpClient);

            Console.WriteLine();

            await AnalyzeBuildsAsync(cache);
        }

        private static HttpClient CreateAzureDevOpsClient(string accountName, string personalAccessToken)
        {
            using var httpClient = new HttpClient();

            var cred = new AuthenticationHeaderValue("BASIC", Convert.ToBase64String(Encoding.ASCII.GetBytes(accountName + ":" + personalAccessToken)));
            httpClient.DefaultRequestHeaders.Authorization = cred;

            var userAgent = new ProductInfoHeaderValue("NugetClientCiAnalysis", "0.1");
            httpClient.DefaultRequestHeaders.UserAgent.Add(userAgent);

            return httpClient;
        }


        public static async Task AnalyzeBuildsAsync(DirectoryInfo cache)
        {
            Dictionary<Week, WeekBuilds> buildsByWeek = new();

            foreach (var zip in cache.GetFiles("*.build.zip"))
            {
                BuildInfo buildInfo;
                using (var stream = zip.OpenRead())
                using (ZipArchive archive = new(stream, ZipArchiveMode.Read))
                {
                    var entry = archive.GetEntry("build.json");
                    using (var entryStream = entry.Open())
                    {
                        buildInfo = await JsonSerializer.DeserializeAsync<BuildInfo>(entryStream);
                    }

                    var sprint = Week.FromDate(buildInfo.finishTime);
                    if (!buildsByWeek.TryGetValue(sprint, out var buildInWeek))
                    {
                        buildInWeek = new WeekBuilds();
                        buildsByWeek.Add(sprint, buildInWeek);
                    }
                    var official = buildInfo.sourceBranch == "refs/heads/dev" && buildInfo.definition.id == 8117;
                    if (official)
                    {
                        buildInWeek.Official.Add(buildInfo);
                    }
                    else
                    {
                        buildInWeek.PullRequest.Add(buildInfo);
                    }
                }
            }

            foreach (var week in buildsByWeek.OrderBy(b => b.Key.sprint).ThenBy(b => b.Key.week))
            {
                var officialCounts = week.Value.Official.GroupBy(b => b.result).ToDictionary(b => b.Key, b => b.Count());
                var prCounts = week.Value.PullRequest.GroupBy(b => b.result).ToDictionary(b => b.Key, b => b.Count());

                Console.WriteLine("{0}.{1}", week.Key.sprint, week.Key.week);
                Console.WriteLine("  Official: " + string.Join(", ", officialCounts.OrderBy(b => b.Key).Select(b => $"{b.Key}({b.Value})")));
                Console.WriteLine("  PRs     :" + string.Join(", ", prCounts.OrderBy(b => b.Key).Select(b => $"{b.Key}({b.Value})")));
                Console.WriteLine();
            }
        }
    }
}
