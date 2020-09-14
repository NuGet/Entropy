using find_buids_in_sprint.Models.AzDO;
using find_buids_in_sprint.Models.ClientCiAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace find_buids_in_sprint
{
    internal class BuildFetcher
    {
        internal static async Task DownloadBuildsAsync(DirectoryInfo cache, HttpClient httpClient)
        {
            BuildList result = await GetBuildsListAsync(cache, httpClient);

            // Azure DevOps can mark builds as retained, so they live much longer than the normal retention policy.
            // We have very few builds retained older than 30 days, not enough to make estimates about build
            // reliability during its week/sprint, so let's just ignore them.
            var oldestBuild = DateTime.Today.AddDays(-30);
            List<BuildInfo> builds = result.value
                .Where(b => b.finishTime >= oldestBuild && b.status == "completed") // Ignore builds still running
                .OrderBy(b => b.finishTime)
                .ToList();

            var summary = new List<Build>(builds.Count);

            for (int i = 0; i < builds.Count; i++)
            {
                await DownloadBuildAsync(builds, i, cache, httpClient);
            }
        }

        private static async Task<BuildList> GetBuildsListAsync(DirectoryInfo cache, HttpClient httpClient)
        {
            BuildList result;
            Dictionary<string, int> buildDefinitions = new Dictionary<string, int>()
            {
                { "official", 8117 },
                { "private", 8118 },
                { "trusted", 14219 }
            };

            var definitionIds = string.Join(",", buildDefinitions.Select(kvp => kvp.Value));

            var url = $"https://dev.azure.com/devdiv/devdiv/_apis/build/builds?definitions={definitionIds}&api-version=5.1";

            var buildsCacheFileInfo = new FileInfo(Path.Combine(cache.FullName, "builds.json"));
            Stopwatch stopwatch = new Stopwatch();
            if (!buildsCacheFileInfo.Exists || buildsCacheFileInfo.LastWriteTimeUtc >= DateTime.UtcNow.AddHours(-1))
            {
                stopwatch.Restart();
                using (Stream stream = await httpClient.GetStreamAsync(url).ConfigureAwait(false))
                using (FileStream file = File.OpenWrite(buildsCacheFileInfo.FullName))
                {
                    await stream.CopyToAsync(file);
                }
                stopwatch.Stop();
                Console.WriteLine("Got builds after {0}", stopwatch.Elapsed.TotalSeconds);
            }

            using (var stream = File.OpenRead(buildsCacheFileInfo.FullName))
            {
                result = await JsonSerializer.DeserializeAsync<BuildList>(stream);
            }
            Console.WriteLine("{0} builds in response", result.value.Count);

            return result;
        }

        private static async Task DownloadBuildAsync(List<BuildInfo> builds, int i, DirectoryInfo cache, HttpClient httpClient)
        {
            Console.WriteLine("Build {0}/{1}", i + 1, builds.Count);
            var stopwatch = Stopwatch.StartNew();

            var ciBuild = builds[i];
            var zipFileName = ciBuild.id + ".build.zip";
            var fileInfo = new FileInfo(Path.Combine(cache.FullName, zipFileName));

            // A build might have completed, we downloaded everything, but then someone clicked the "rerun failed jobs" button.
            // We should update our copy in that case.
            if (!fileInfo.Exists || ciBuild.finishTime != fileInfo.LastWriteTimeUtc)
            {
                var tmpFileName = Path.Combine(cache.FullName, ciBuild.id + ".tmp.zip");
                Timeline[] timelines;
                using (var fileStream = File.Create(tmpFileName))
                {
                    if (fileStream.Length != 0)
                    {
                        fileStream.SetLength(0);
                    }

                    using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: true))
                    {
                        BuildInfo buildInfo;
                        string url = $"https://dev.azure.com/devdiv/devdiv/_apis/build/builds/{builds[i].id}?api-version=6.0";
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var downloadStream = await httpClient.GetStreamAsync(url))
                            {
                                await downloadStream.CopyToAsync(memoryStream);
                            }

                            memoryStream.Position = 0;
                            var zipEntry = zipArchive.CreateEntry("build.json");
                            using (var entryStream = zipEntry.Open())
                            {
                                await memoryStream.CopyToAsync(entryStream);
                            }

                            memoryStream.Position = 0;
                            buildInfo = await JsonSerializer.DeserializeAsync<Models.AzDO.BuildInfo>(memoryStream);
                        }

                        if (!buildInfo.validationResults.Any(v => v.result == "error"))
                        {
                            url = buildInfo.links["timeline"].href;
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var downloadStream = await httpClient.GetStreamAsync(url))
                                {
                                    await downloadStream.CopyToAsync(memoryStream);
                                }

                                memoryStream.Position = 0;
                                var timeline = await JsonSerializer.DeserializeAsync<Timeline>(memoryStream);
                                var attempt = timeline.records.Max(r => r.attempt);
                                timelines = new Timeline[attempt];
                                timelines[^1] = timeline;

                                memoryStream.Position = 0;
                                var zipEntry = zipArchive.CreateEntry($"attempt/{attempt}.json");
                                using (var entryStream = zipEntry.Open())
                                {
                                    await memoryStream.CopyToAsync(entryStream);
                                }
                            }
                            var recordWithMaxAttempts = timelines[^1].records.First(r => r.previousAttempts.Count == timelines.Length - 1);
                            foreach (var previousAttempt in recordWithMaxAttempts.previousAttempts)
                            {
                                url = $"https://dev.azure.com/devdiv/devdiv/_apis/build/builds/{builds[i].id}/timeline/{previousAttempt.timelineId}?api-version=6.0";
                                using (var memoryStream = new MemoryStream())
                                {
                                    using (var downloadStream = await httpClient.GetStreamAsync(url))
                                    {
                                        await downloadStream.CopyToAsync(memoryStream);
                                    }

                                    memoryStream.Position = 0;
                                    timelines[previousAttempt.attempt - 1] = await JsonSerializer.DeserializeAsync<Timeline>(memoryStream);

                                    memoryStream.Position = 0;
                                    var zipEntry = zipArchive.CreateEntry($"attempt/{previousAttempt.attempt}.json");
                                    using (var entryStream = zipEntry.Open())
                                    {
                                        await memoryStream.CopyToAsync(entryStream);
                                    }
                                }
                            }
                        }
                        else
                        {
                            timelines = Array.Empty<Timeline>();
                        }
                    }
                }

                var logsZipPath = Path.Combine(cache.FullName, ciBuild.id + ".logs.zip");
                using (var zipStream = File.Create(logsZipPath))
                {
                    if (zipStream.Length != 0)
                    {
                        zipStream.SetLength(0);
                    }

                    using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                    {
                        var logs = timelines.SelectMany(r => r.records)
                            .Where(r => r.type == "Task" && r.log != null)
                            .Select(r => r.log)
                            .GroupBy(l => l.id)
                            .Select(l => l.First())
                            .ToList();
                        for (int l = 0; l < logs.Count; l++)
                        {
                            Console.Write("Log {0}/{1}", l, logs.Count);
                            Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
                            var log = logs[l];
                            var zipEntry = zipArchive.CreateEntry($"{log.id}.txt");
                            using (var entryStream = zipEntry.Open())
                            {
                                for (int retry = 0; retry < 5; retry++)
                                {
                                    try
                                    {
                                        using (var downloadStream = await httpClient.GetStreamAsync(log.url))
                                        {
                                            await downloadStream.CopyToAsync(entryStream);
                                        }
                                        break;
                                    }
                                    catch (HttpRequestException exception)
                                    {
                                        if (exception.StatusCode != System.Net.HttpStatusCode.InternalServerError)
                                        {
                                            throw;
                                        }

                                        await Task.Delay(1000);
                                    }
                                }
                            }
                        }
                    }
                }

                // In order to detect when a failed job is rerun, set the zip last write time to the build finish time.
                // This allows us to avoid opening the zip, parsing the build json, to compare dates.
                File.SetLastWriteTime(logsZipPath, ciBuild.finishTime);
                File.SetLastWriteTime(tmpFileName, ciBuild.finishTime);
                File.Move(tmpFileName, fileInfo.FullName, overwrite: true);
            }

            stopwatch.Stop();
            Console.SetCursorPosition(0, Console.GetCursorPosition().Top - 1);
            Console.WriteLine("Build {0}/{1} ({2})", i + 1, builds.Count, stopwatch.Elapsed);
        }
    }
}
