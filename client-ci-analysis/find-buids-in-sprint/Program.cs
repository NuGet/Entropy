using find_buids_in_sprint.Models.AzDO;
using find_buids_in_sprint.Models.ClientCiAnalysis;
using NetworkManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var sprintEpoch = new DateTimeOffset(2010, 07, 26, 0, 0, 0, TimeSpan.FromHours(-7));

            var sprint = 177;

            var startTime = sprintEpoch.AddDays(7.0 * 3.0 * sprint);
            var endTime = startTime.AddDays(7.0 * 3.0);

            var accountName = Environment.GetEnvironmentVariable("AzDO_ACCOUNT");
            var personalAccessToken = Environment.GetEnvironmentVariable("AzDO_PAT");

            if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(personalAccessToken))
            {
                Console.WriteLine("Set AzDO_ACCOUNT and AzDO_PAT environment variables.");
                Console.WriteLine("Project properties -> Debug in VS");
                Console.WriteLine("launchSettings.json in VSCode");
                return;
            }

            var userAgent = new ProductInfoHeaderValue("NugetClientCiAnalysis", "0.1"); 
            var cacheDirectory = Path.Combine(Path.GetTempPath(), "nuget-client-ci-analysis");
            var httpManagerOptions = new HttpManagerOptions(userAgent, cacheDirectory);

            using var httpManager = await HttpManager.CreateAsync(httpManagerOptions);

            var cred = new AuthenticationHeaderValue("BASIC", Convert.ToBase64String(Encoding.ASCII.GetBytes(accountName + ":" + personalAccessToken)));
            httpManager.AddCredential("https://dev.azure.com/DevDiv/", cred);


            Dictionary<string, int> buildDefinitions = new Dictionary<string, int>()
            {
                { "official", 8117 },
                { "private", 8118 }
            };

            foreach ((string name, int buildId) in buildDefinitions)
            {
                var url = $"https://dev.azure.com/devdiv/devdiv/_apis/build/builds?definitions={buildId}&api-version=5.1";

                BuildList result;
                using (Stream stream = await httpManager.GetAsync(url, maxCacheAge: TimeSpan.FromHours(24)).ConfigureAwait(false))
                {
                    result = await System.Text.Json.JsonSerializer.DeserializeAsync<BuildList>(stream);
                }

                var builds = result.value
                    .Where(b => b.finishTime >= startTime && b.finishTime <= endTime)
                    //.Where(b => b.result == "failed" || b.result == "canceled")
                    .ToList();

                var summary = new List<Build>(builds.Count);

                for (int i = 0; i < builds.Count; i++)
                {
                    var ciBuild = builds[i];
                    var analysisBuild = await GetBuildAsync(ciBuild, httpManager);
                    summary.Add(analysisBuild);
                }

                var options = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };

                using (var fileStream = File.OpenWrite($"{name}_builds.json"))
                {
                    await JsonSerializer.SerializeAsync(fileStream, summary, options);
                    fileStream.SetLength(fileStream.Position);
                }

                Console.WriteLine(name);
                Console.WriteLine($"builds = " + summary.Count);
                Console.WriteLine($"jobs = " + summary.SelectMany(b => b.jobs).Count());
                Console.WriteLine($"attempts = " + summary.Sum(b =>
                {
                    if (b.jobs != null && b.jobs.Count > 0)
                    {
                        return b.jobs.Max(j => j.Value.Count);
                    }
                    else
                    {
                        return 1;
                    }
                }));

                Console.WriteLine();
            }
        }

        private static async Task<Build> GetBuildAsync(BuildInfo ciBuild, HttpManager httpManager)
        {
            string timelineUrl;
            try
            {
                var analysisBuild = new Build()
                {
                    id = ciBuild.id,
                    buildNumber = ciBuild.buildNumber,
                    url = ciBuild.links["web"].href,
                    result = ciBuild.result,
                    finishTime = ciBuild.finishTime,
                    jobs = new Dictionary<string, List<Attempt>>()
                };

                if (!ciBuild.validationResults.Any(r => string.Equals("error", r.result, StringComparison.OrdinalIgnoreCase)))
                {
                    Timeline[] timelines;

                    {
                        timelineUrl = ciBuild.links["timeline"].href;
                        Timeline timeline;
                        using (var responseStream = await httpManager.GetAsync(timelineUrl))
                        {
                            timeline = await System.Text.Json.JsonSerializer.DeserializeAsync<Timeline>(responseStream);
                        }

                        var stage = timeline.records.Single(r => r.parentId == null);
                        timelines = new Timeline[stage.attempt];
                        timelines[stage.attempt - 1] = timeline;

                        while (stage.attempt > 1)
                        {
                            var prevTimelineId = stage.previousAttempts.Where(a => a.attempt == stage.attempt - 1).Single().timelineId;
                            timelineUrl = ciBuild.links["timeline"].href + "/" + prevTimelineId;
                            using (var responseStream = await httpManager.GetAsync(timelineUrl))
                            {
                                timeline = await System.Text.Json.JsonSerializer.DeserializeAsync<Timeline>(responseStream);
                            }
                            stage = timeline.records.Single(r => r.parentId == null);
                            timelines[stage.attempt - 1] = timeline;
                        }
                    }

                    for (int i = 0; i < timelines.Length; i++)
                    {
                        var attemptNumber = i + 1;
                        foreach (var job in timelines[i].records.Where(r => r.attempt == attemptNumber && string.Equals("job", r.type, StringComparison.OrdinalIgnoreCase)))
                        {
                            var jobName = job.name;

                            if (!analysisBuild.jobs.TryGetValue(jobName, out List<Attempt> attempts))
                            {
                                attempts = new List<Attempt>();
                                analysisBuild.jobs.Add(jobName, attempts);
                            }

                            var attempt = new Attempt()
                            {
                                result = job.result
                            };

                            if (job.startTime != null && job.finishTime != null)
                            {
                                attempt.duration = (job.finishTime.Value - job.startTime.Value).ToString("g");
                            }

                            attempts.Add(attempt);
                        }
                    }
                }

                return analysisBuild;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
